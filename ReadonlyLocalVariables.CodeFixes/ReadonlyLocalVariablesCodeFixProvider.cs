
// (c) 2022 Kazuki KOHZUKI

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
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
            => ImmutableArray.Create("RO0001");

        override public FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        override public async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var assignment = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<AssignmentExpressionSyntax>().First();

            // Creates new local localDeclaration statement to remove assignment expression.
            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.NewVariable,
                    c => MakeNewVariable(context.Document, assignment, c),
                    nameof(CodeFixResources.NewVariable)
                ),
                diagnostic
            );

            // Adds an attribute to allow reassignment.
            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.AddAttribute,
                    c => AddAttribute(context.Document, assignment, c),
                    nameof(CodeFixResources.AddAttribute)
                ),
                diagnostic
            );
        } // override public Task RegisterCodeFixesAsync (CodeFixContext)

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
        }

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
            var oldRightOperand = assignment.Right;
            
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var oldName = oldLeftOperand.ChildTokens().First().ValueText;
            var newName = CreateNewUniqueName(oldName, semanticModel, assignment.SpanStart);

            var newRightOperand = assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
                ? oldRightOperand
                : SyntaxFactory.BinaryExpression(
                    kind : GetSimpleExpressionKind(assignment.Kind()),
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
            var trackingRoot = oldRoot.TrackNodes(assignmentStatement);
            var newAssignmentStatement = trackingRoot.GetCurrentNode(assignmentStatement);
            var newRoot = trackingRoot.ReplaceNode(newAssignmentStatement, formattedDeclaration);
            var newLen = formattedDeclaration.ToString().Length;

            var renameStartPosition = assignmentStatement.GetTrailingTrivia().Span.End - oldLen + newLen;
            var rewriter = new IdentifierNameRewriter(renameStartPosition, oldName, newName);
            var oldMethod = GetMinimumScope(newRoot.FindToken(renameStartPosition).Parent, out var _);
            var newMethod = rewriter.Visit(oldMethod);

            return document.WithSyntaxRoot(newRoot.ReplaceNode(oldMethod, newMethod));
        } // private static async Task<Document> MakeNewVariable (Document, AssignmentExpressionSyntax, CancellationToken)

        /// <summary>
        /// Gets <see cref="SyntaxKind"/> of simple expression corresponds to the given compound assignment expression.
        /// </summary>
        /// <param name="assignmentType">The <see cref="SyntaxKind"/> of compound assignment expression.</param>
        /// <returns>The <see cref="SyntaxKind"/> of simple expression.
        /// If <paramref name="assignmentType"/> does not represent compound assignment, <see cref="SyntaxKind.None"/>.</returns>
        /// <remarks>Although bitwise logical products and disjunctions of value types and logical types can be distinguished as different <see cref="SyntaxKind"/>,
        /// they are not distinguished as token sequences, and thus can be treated as either operation.
        /// Therefore, if logical and expression or logical or expression should be the return value,
        /// <see cref="SyntaxKind.BitwiseAndExpression"/> or <see cref="SyntaxKind.BitwiseOrExpression"/> is returned respectively.</remarks>
        private static SyntaxKind GetSimpleExpressionKind(SyntaxKind assignmentType)
            => assignmentType switch
            {
                SyntaxKind.AddAssignmentExpression         => SyntaxKind.AddExpression,
                SyntaxKind.AndAssignmentExpression         => SyntaxKind.BitwiseAndExpression,
                SyntaxKind.CoalesceAssignmentExpression    => SyntaxKind.CoalesceExpression,
                SyntaxKind.DivideAssignmentExpression      => SyntaxKind.DivideExpression,
                SyntaxKind.ExclusiveOrAssignmentExpression => SyntaxKind.ExclusiveOrExpression,
                SyntaxKind.LeftShiftAssignmentExpression   => SyntaxKind.LeftShiftExpression,
                SyntaxKind.ModuloAssignmentExpression      => SyntaxKind.ModuloExpression,
                SyntaxKind.MultiplyAssignmentExpression    => SyntaxKind.MultiplyExpression,
                SyntaxKind.OrAssignmentExpression          => SyntaxKind.BitwiseOrExpression,
                SyntaxKind.RightShiftAssignmentExpression  => SyntaxKind.RightShiftAssignmentExpression,
                SyntaxKind.SubtractAssignmentExpression    => SyntaxKind.SubtractExpression,
                _ => SyntaxKind.None,
            };

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


        private static async Task<Document> AddAttribute(Document document, AssignmentExpressionSyntax assignment, CancellationToken cancellationToken)
        {
            var node = assignment.Parent;

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var compilationUnitSyntax = (CompilationUnitSyntax)oldRoot.TrackNodes(node);
            var usings = compilationUnitSyntax.Usings.Select(directive => directive.Name.ToString());
            if (!usings.Where(name => name == nameof(ReadonlyLocalVariables)).Any())
            {
                var name = SyntaxFactory.IdentifierName(nameof(ReadonlyLocalVariables));
                compilationUnitSyntax = compilationUnitSyntax.AddUsings(SyntaxFactory.UsingDirective(name));
            }

            node = GetMinimumScope(compilationUnitSyntax.GetCurrentNode(node), out var isClosure);
            if (node == null) return document.WithSyntaxRoot(compilationUnitSyntax);

            var leftOperand = assignment.Left;
            var oldName = leftOperand.ChildTokens().First().ValueText;
            var attrName = SyntaxFactory.IdentifierName("ReassignableVariable");
            var attrParam = SyntaxFactory.AttributeArgument(null, null, SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(oldName)));
            var attrParams = SyntaxFactory.AttributeArgumentList(new SeparatedSyntaxList<AttributeArgumentSyntax>().Add(attrParam));
            var attribute = SyntaxFactory.Attribute(attrName, attrParams);
            var attributeList = SyntaxFactory.AttributeList(new SeparatedSyntaxList<AttributeSyntax>().Add(attribute));

            SyntaxNode newMethodDeclaration = isClosure ? (node as LocalFunctionStatementSyntax).AddAttributeLists(attributeList)
                                                        : (node as MethodDeclarationSyntax).AddAttributeLists(attributeList);
            var newRoot = compilationUnitSyntax.ReplaceNode(node, newMethodDeclaration.WithLeadingTrivia(node.GetLeadingTrivia()));

            return document.WithSyntaxRoot(newRoot);
        } // private static async Task<Document> AddAttribute (Document, AssignmentExpressionSyntax, CancellationToken)
    } // public class ReadonlyLocalVariablesCodeFixProvider : CodeFixProvider
} // namespace ReadonlyLocalVariables
