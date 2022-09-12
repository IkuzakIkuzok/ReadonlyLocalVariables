﻿
// (c) 2022 Kazuki KOHZUKI

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ReadonlyLocalVariables
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ReadonlyLocalVariablesCodeFixProvider)), Shared]
    public class ReadonlyLocalVariablesCodeFixProvider : CodeFixProvider
    {
        private static readonly Regex re_identifierSuffix = new(@"(\d+)$");

        override public ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(ReadonlyLocalVariablesAnalyzer.DiagnosticId);

        override public FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        override public async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var nodes = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf();

            void RegisterCodeFixes<TSyntaxNode>(
                Func<Document, TSyntaxNode, CancellationToken, Task<Document>> newVariable,
                Func<Document, TSyntaxNode, CancellationToken, Task<Document>> addAttribute
            ) where TSyntaxNode : CSharpSyntaxNode
            {
                var syntaxNodes = nodes.OfType<TSyntaxNode>();
                if (!syntaxNodes.Any()) return;
                var syntaxNode = syntaxNodes.First();

                // Creates new local variable to remove assignment expression.
                context.RegisterCodeFix(
                    CodeAction.Create(
                        CodeFixResources.NewVariable,
                        c => newVariable(context.Document, syntaxNode, c),
                        nameof(CodeFixResources.NewVariable)
                    ),
                    diagnostic
                );

                // Adds an attribute to allow reassignment.
                context.RegisterCodeFix(
                    CodeAction.Create(
                        CodeFixResources.AddAttribute,
                        c => addAttribute(context.Document, syntaxNode, c),
                        nameof(CodeFixResources.AddAttribute)
                    ),
                    diagnostic
                );
            }

            RegisterCodeFixes<AssignmentExpressionSyntax>(MakeNewVariable, AddAttribute);
            RegisterCodeFixes<ArgumentSyntax>(MakeNewVariable, AddAttribute);
        } // override public Task RegisterCodeFixesAsync (CodeFixContext)

        /// <summary>
        /// Finds the smallest scope that includes the specified node.
        /// </summary>
        /// <param name="node">The target node.</param>
        /// <param name="isLocal">A value indicating whether the scope is a local function.</param>
        /// <returns>Scope that includes <paramref name="node"/>.</returns>
        private static SyntaxNode GetMinimumScope(SyntaxNode node, out bool isLocal)
        {
            isLocal = false;
            while (!(node is MethodDeclarationSyntax || node is LocalFunctionStatementSyntax))
            {
                var parent = node.Parent;
                if (parent == null)
                    return null;
                node = parent;
            }
            isLocal = node is LocalFunctionStatementSyntax;
            return node;
        } // private static SyntaxNode GetMinimumScope (SyntaxNode, out bool)

        /// <summary>
        /// Checks to see if an attribute is set that allows reassignment to the variable.
        /// </summary>
        /// <param name="variable">A tuple of variable node and its correspond symbol.</param>
        /// <param name="semanticModel">The semantic model to get symbol information.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns><c>true</c> if the variable is reassignable; otherwise, <c>false</c>.</returns>
        private static bool CheckIfVariableIsNotReassignable((SyntaxNode node, ISymbol symbol) variable, SemanticModel semanticModel, CancellationToken cancellationToken)
            => ReadonlyLocalVariablesAnalyzer.CheckIfVariableIsNotReassignable(variable.symbol, variable.node, semanticModel, cancellationToken).Result;

        #region new variable

        /// <summary>
        /// Adds a new variable declaration instead of reassignment.
        /// </summary>
        /// <param name="document">The documentation to be rewritten.</param>
        /// <param name="assignment">The assignment to be rewritten.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The <see cref="Task"/> object representing the asynchronous operation.</returns>
        private static async Task<Document> MakeNewVariable(Document document, AssignmentExpressionSyntax assignment, CancellationToken cancellationToken)
        {
            /*
             * LocalDeclarationStatement           var i = 0
             *      VariableDeclaration
             *          IdentifierName             
             *              IdentifierToken        var
             *          VariableDeclartor
             *              IdentifierToken        i
             *              EqualsValueClause
             *                  EqualsToken        =
             *                  {Expression}       0
             *      SemicolonToken                 ;
             */

            var assignmentStatement = assignment.Parent;
            var oldLen = assignmentStatement.ToString().Length;

            var oldLeftOperand = assignment.Left;
            if (oldLeftOperand is TupleExpressionSyntax syntax)
                return await RewriteTuple(syntax, document, cancellationToken);

            var oldRightOperand = assignment.Right;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var oldName = oldLeftOperand.ChildTokens().First().ValueText;
            var newName = CreateNewUniqueName(oldName, semanticModel, assignment.SpanStart);

            var rightType = semanticModel.GetTypeInfo(oldRightOperand, cancellationToken);
            var newRightOperand = assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
                ? oldRightOperand
                : SyntaxFactory.BinaryExpression(
                    kind : GetSimpleAssignmentExpressionKind(assignment.Kind(), rightType),
                    left : oldLeftOperand,
                    right: oldRightOperand
                  );

            var declarator = SyntaxFactory.VariableDeclarator(
                identifier  : SyntaxFactory.Identifier(newName),
                argumentList: null,
                initializer : SyntaxFactory.EqualsValueClause(newRightOperand)
            );
            var declaration = SyntaxFactory.VariableDeclaration(
                type     : SyntaxFactory.IdentifierName("var"),
                variables: SyntaxFactory.SeparatedList(new[] { declarator })
            );
            var localDeclaration = SyntaxFactory.LocalDeclarationStatement(declaration);

            var formattedDeclaration = localDeclaration.WithAdditionalAnnotations(Formatter.Annotation)
                                                       .WithTriviaFrom(assignmentStatement);
            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = oldRoot.ReplaceNode(assignmentStatement, formattedDeclaration);
            var newLen = formattedDeclaration.ToString().Length;

            var renameStartPosition = assignmentStatement.GetTrailingTrivia().Span.End - oldLen + newLen;
            var rewriter = new IdentifierNameRewriter(renameStartPosition, oldName, newName);
            var oldMethod = GetMinimumScope(newRoot.FindToken(renameStartPosition).Parent, out var _);
            var newMethod = rewriter.Visit(oldMethod);

            return document.WithSyntaxRoot(newRoot.ReplaceNode(oldMethod, newMethod));
        } // private static async Task<Document> MakeNewVariable (Document, AssignmentExpressionSyntax, CancellationToken)

        /// <summary>
        /// Gets <see cref="SyntaxKind"/> of simple assignment expression corresponds to the given compound assignment expression.
        /// </summary>
        /// <param name="assignmentType">The <see cref="SyntaxKind"/> of compound assignment expression.</param>
        /// <param name="type">Type information of right operand.</param>
        /// <returns>The <see cref="SyntaxKind"/> of simple assignment expression corresponds to the given expression.</returns>
        private static SyntaxKind GetSimpleAssignmentExpressionKind(SyntaxKind assignmentType, TypeInfo? type)
        {
            var isLogical = type?.Type.Name == nameof(Boolean);
            return assignmentType switch
            {
                SyntaxKind.AddAssignmentExpression         => SyntaxKind.AddExpression,
                SyntaxKind.AndAssignmentExpression         => isLogical ? SyntaxKind.LogicalAndExpression : SyntaxKind.BitwiseAndExpression,
                SyntaxKind.CoalesceAssignmentExpression    => SyntaxKind.CoalesceExpression,
                SyntaxKind.DivideAssignmentExpression      => SyntaxKind.DivideExpression,
                SyntaxKind.ExclusiveOrAssignmentExpression => SyntaxKind.ExclusiveOrExpression,
                SyntaxKind.LeftShiftAssignmentExpression   => SyntaxKind.LeftShiftExpression,
                SyntaxKind.ModuloAssignmentExpression      => SyntaxKind.ModuloExpression,
                SyntaxKind.MultiplyAssignmentExpression    => SyntaxKind.MultiplyExpression,
                SyntaxKind.OrAssignmentExpression          => isLogical ? SyntaxKind.LogicalOrExpression : SyntaxKind.BitwiseOrExpression,
                SyntaxKind.RightShiftAssignmentExpression  => SyntaxKind.RightShiftAssignmentExpression,
                SyntaxKind.SubtractAssignmentExpression    => SyntaxKind.SubtractExpression,
                _ => assignmentType,
            };
        } // private static SyntaxKind GetSimpleAssignmentExpressionKind (SyntaxKind, [TypeInfo?])

        /// <summary>
        /// Rewrites tuple elements.
        /// </summary>
        /// <param name="tuple">The tuple to be rewritten.</param>
        /// <param name="document">The documentation to be rewritten.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The <see cref="Task"/> object representing the asynchronous operation.</returns>
        private static async Task<Document> RewriteTuple(TupleExpressionSyntax tuple, Document document, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var variables = tuple.Arguments.Select(argument => argument.ChildNodes().First())
                                           .Where(node => node is not DeclarationExpressionSyntax)
                                           .Select(node => (node, semanticModel.GetSymbolInfo(node).Symbol))
                                           .Where(variable => CheckIfVariableIsNotReassignable(variable, semanticModel, cancellationToken));

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            root = root.TrackNodes(variables.Select((variable) => variable.node));
            foreach ((var node, var symbol) in variables)
            {
                var trackedNode = root.GetCurrentNode(node);

                var oldLen = trackedNode.ToString().Length;
                var oldName = symbol.Name;
                var newName = CreateNewUniqueName(oldName, semanticModel, trackedNode.SpanStart);
                var declaration = SyntaxFactory.DeclarationExpression(
                    type       : SyntaxFactory.IdentifierName("var"),
                    designation: SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(newName))
                );
                var newLen = declaration.ToString().Length;

                root = root.ReplaceNode(trackedNode, declaration);

                var renameStartPosition = trackedNode.Span.End - oldLen + newLen;
                var rewriter = new IdentifierNameRewriter(renameStartPosition, oldName, newName);
                var oldMethod = GetMinimumScope(root.FindToken(renameStartPosition).Parent, out var _);
                var newMethod = rewriter.Visit(oldMethod);
                root = root.ReplaceNode(oldMethod, newMethod);
            }

            return document.WithSyntaxRoot(root);
        } // private static async Task<Document> RewriteTuple (TupleExpressionSyntax, Document, CancellationToken)

        /// <summary>
        /// Rewrite the argument using `out` to a new variable declaration using `out var`.
        /// </summary>
        /// <param name="document">The documentation to be rewritten.</param>
        /// <param name="argument">The argument to be rewritten.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The <see cref="Task"/> object representing the asynchronous operation.</returns>
        private static async Task<Document> MakeNewVariable(Document document, ArgumentSyntax argument, CancellationToken cancellationToken)
        {
            var variable = argument.GetLastToken();
            var oldLen = argument.ToString().Length;

            var oldExpr = argument.ChildNodes().Last();

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var oldName = variable.ValueText;
            var newName = CreateNewUniqueName(oldName, semanticModel, argument.SpanStart);

            var declaration = SyntaxFactory.DeclarationExpression(
                type: SyntaxFactory.IdentifierName("var"),
                designation: SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(newName))
            );
            var newArgument = argument.ReplaceNode(oldExpr, declaration).WithTriviaFrom(argument);

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var trackingRoot = oldRoot.TrackNodes(argument);
            var oldArgument = trackingRoot.GetCurrentNode(argument);
            var newRoot = trackingRoot.ReplaceNode(oldArgument, newArgument);
            var newLen = newArgument.ToString().Length;

            var renameStartPosition = argument.Span.End - oldLen + newLen;
            var rewriter = new IdentifierNameRewriter(renameStartPosition, oldName, newName);
            var oldMethod = GetMinimumScope(newRoot.FindToken(renameStartPosition).Parent, out var _);
            var newMethod = rewriter.Visit(oldMethod);

            return document.WithSyntaxRoot(newRoot.ReplaceNode(oldMethod, newMethod));
        } // private static async Task<Document> MakeNewVariable (Document, ArgumentSyntax, CancellationToken)

        /// <summary>
        /// Creates a new unique identifier name based on the specified position.
        /// </summary>
        /// <param name="oldName">The old name.</param>
        /// <param name="semanticModel">The semantic model for verifying uniqueness of identifier names.</param>
        /// <param name="start">A reference position for verifying uniqueness.</param>
        /// <returns>A created identifier name.</returns>
        private static string CreateNewUniqueName(string oldName, SemanticModel semanticModel, int start)
        {
            var m = re_identifierSuffix.Match(oldName);
            var s = m.Success ? m.Value : string.Empty;
            var basename = oldName.Substring(0, oldName.Length - s.Length);
            var suffix = string.IsNullOrEmpty(s) ? 0 : int.Parse(s);
            do
            {
                var newName = $"{basename}{++suffix}";
                if (semanticModel.LookupSymbols(start, container: null, name: newName).IsEmpty)
                    return newName;
            } while (true);
        } // private static string CreateNewUniqueName (string, SemanticModel, int)

        #endregion new variable

        #region add attribute

        /// <summary>
        /// Adds a ReassignableVariableAttribute to the scope containing the assignment statement.
        /// </summary>
        /// <param name="document">The documentation to be rewritten.</param>
        /// <param name="assignment">The assignment to be checked.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The <see cref="Task"/> object representing the asynchronous operation.</returns>
        private static async Task<Document> AddAttribute(Document document, AssignmentExpressionSyntax assignment, CancellationToken cancellationToken)
        {
            var node = assignment.Parent;
            var leftOperand = assignment.Left;
            if (leftOperand is TupleExpressionSyntax tuple)
                return await AddAttribute(document, tuple, cancellationToken);

            var name = leftOperand.ChildTokens().First().ValueText;
            return await AddAttribute(document, node, name, cancellationToken);
        } // private static async Task<Document> AddAttribute (Document, AssignmentExpressionSyntax, CancellationToken)

        /// <summary>
        /// Adds a ReassignableVariableAttribute to the scope containing the tuple expression.
        /// </summary>
        /// <param name="document">The documentation to be rewritten.</param>
        /// <param name="tuple">The tuple to be checked.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The <see cref="Task"/> object representing the asynchronous operation.</returns>
        private static async Task<Document> AddAttribute(Document document, TupleExpressionSyntax tuple, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var names = tuple.Arguments.Select(argument => argument.ChildNodes().First())
                                       .Where(node => node is not DeclarationExpressionSyntax)
                                       .Select(node => (node, semanticModel.GetSymbolInfo(node).Symbol))
                                       .Where(variable => CheckIfVariableIsNotReassignable(variable, semanticModel, cancellationToken))
                                       .Select(variable => variable.Symbol.Name);

            return await AddAttribute(document, tuple.Parent, names, cancellationToken);
        } // private static async Task<Document> AddAttribute (Document, TupleExpressionSyntax, CancellationToken)

        /// <summary>
        /// Adds a ReassignableVariableAttribute to the scope using the `out` argument.
        /// </summary>
        /// <param name="document">The documentation to be rewritten.</param>
        /// <param name="argument">The argument to be checked.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The <see cref="Task"/> object representing the asynchronous operation.</returns>
        private static async Task<Document> AddAttribute(Document document, ArgumentSyntax argument, CancellationToken cancellationToken)
        {
            var node = argument.Parent;
            var variable = argument.GetLastToken();
            var name = variable.ValueText;

            return await AddAttribute(document, node, name, cancellationToken);
        } // private static async Task<Document> AddAttribute (Document, ArgumentSyntax, CancellationToken)

        /// <summary>
        /// Adds a ReassignableVariableAttribute to the scope containing the specified node to allow reassignment.
        /// </summary>
        /// <param name="document">The documentation to be rewritten.</param>
        /// <param name="node">The target node.</param>
        /// <param name="name">The name of identifier.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The <see cref="Task"/> object representing the asynchronous operation.</returns>
        private static async Task<Document> AddAttribute(Document document, SyntaxNode node, string name, CancellationToken cancellationToken)
            => await AddAttribute(document, node, new[] { name }, cancellationToken);

        /// <summary>
        /// Adds a ReassignableVariableAttribute to the scope containing the specified node to allow reassignments.
        /// </summary>
        /// <param name="document">The documentation to be rewritten.</param>
        /// <param name="node">The target node.</param>
        /// <param name="names">The names of identifiers.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The <see cref="Task"/> object representing the asynchronous operation.</returns>
        private static async Task<Document> AddAttribute(Document document, SyntaxNode node, IEnumerable<string> names, CancellationToken cancellationToken)
        {
            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var compilationUnitSyntax = (CompilationUnitSyntax)oldRoot.TrackNodes(node);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var usings = semanticModel.GetImportScopes(node.SpanStart, cancellationToken)
                                      .SelectMany(importScope => importScope.Imports)
                                      .Select(nsOrType => nsOrType.NamespaceOrType.Name);
            
            if (!usings.Where(ns => ns == nameof(ReadonlyLocalVariables)).Any())
            {
                var ns = SyntaxFactory.IdentifierName(nameof(ReadonlyLocalVariables));
                compilationUnitSyntax = compilationUnitSyntax.AddUsings(SyntaxFactory.UsingDirective(ns));
            }

            node = GetMinimumScope(compilationUnitSyntax.GetCurrentNode(node), out var isClosure);
            if (node == null) return document.WithSyntaxRoot(compilationUnitSyntax);

            static AttributeArgumentSyntax GetAttributeArgument(string name)
                => SyntaxFactory.AttributeArgument(null, null, SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(name)));

            var attrName = SyntaxFactory.IdentifierName("ReassignableVariable");
            var attrParams = SyntaxFactory.AttributeArgumentList(new SeparatedSyntaxList<AttributeArgumentSyntax>().AddRange(names.Select(name => GetAttributeArgument(name))));
            var attribute = SyntaxFactory.Attribute(attrName, attrParams);
            var attributeList = SyntaxFactory.AttributeList(new SeparatedSyntaxList<AttributeSyntax>().Add(attribute));

            SyntaxNode newMethodDeclaration = isClosure ? (node as LocalFunctionStatementSyntax).AddAttributeLists(attributeList)
                                                        : (node as MethodDeclarationSyntax).AddAttributeLists(attributeList);
            var newRoot = compilationUnitSyntax.ReplaceNode(node, newMethodDeclaration.WithLeadingTrivia(node.GetLeadingTrivia()));

            return document.WithSyntaxRoot(newRoot);
        } // private static async Task<Document> AddAttribute (Document document, SyntaxNode node, IEnumerable<string> names, CancellationToken cancellationToken)

        #endregion  add attribute
    } // public class ReadonlyLocalVariablesCodeFixProvider : CodeFixProvider
} // namespace ReadonlyLocalVariables
