
// (c) 2022 Kazuki KOHZUKI

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace ReadonlyLocalVariables
{
    /// <summary>
    /// Analyzes reassignments to local variables.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ReadonlyLocalVariablesAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// Diagnostic ID.
        /// </summary>
        public static readonly string DiagnosticId = "RO0001";

        private static LocalizableResourceString GetLocalizableResourceString(string resourceName)
            => new(resourceName, AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
        private static readonly LocalizableResourceString title = GetLocalizableResourceString(nameof(AnalyzerResources.AnalyzerTitle));
        private static readonly LocalizableResourceString messageFormat = GetLocalizableResourceString(nameof(AnalyzerResources.AnalyzerMessageFormat));

        private const string Category = "CodeStyle";

        private static readonly DiagnosticDescriptor Rule = new(
            id                : DiagnosticId,
            title             : title,
            messageFormat     : messageFormat,
            category          : Category,
            defaultSeverity   : DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        override public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        override public void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeAssignmentNode,
                SyntaxKind.AddAssignmentExpression,
                SyntaxKind.AndAssignmentExpression,
                SyntaxKind.CoalesceAssignmentExpression,
                SyntaxKind.DivideAssignmentExpression,
                SyntaxKind.ExclusiveOrAssignmentExpression,
                SyntaxKind.LeftShiftAssignmentExpression,
                SyntaxKind.ModuloAssignmentExpression,
                SyntaxKind.MultiplyAssignmentExpression,
                SyntaxKind.OrAssignmentExpression,
                SyntaxKind.RightShiftAssignmentExpression,
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxKind.SubtractAssignmentExpression
            );

            context.RegisterSyntaxNodeAction(AnalyzeOutParameterNode,
                SyntaxKind.Argument
            );
        } // override public void Initialize (AnalysisContext)

        /// <summary>
        /// Analyzes <see cref="AssignmentExpressionSyntax"/> node.
        /// </summary>
        /// <param name="context">Context to analyze.</param>
        private static void AnalyzeAssignmentNode(SyntaxNodeAnalysisContext context)
        {
            var semanticModel = context.SemanticModel;
            if (semanticModel == null) return;

            var node = (AssignmentExpressionSyntax)context.Node;
            if (node.Parent is ForStatementSyntax) return;
            var leftNode = node.Left;
            if (leftNode is TupleExpressionSyntax tuple)
            {
                CheckTupleNode(tuple, context);
                return;
            }
            var leftSymbol = semanticModel.GetSymbolInfo(leftNode).Symbol;
            ReportIfNecessary(leftSymbol, node, context);
        } // private static void AnalyzeAssignmentNode (SyntaxNodeAnalysisContext)

        /// <summary>
        /// Checks tuple node.
        /// </summary>
        /// <param name="tuple">The tuple node to check.</param>
        /// <param name="context">The context to check.</param>
        private static void CheckTupleNode(TupleExpressionSyntax tuple, SyntaxNodeAnalysisContext context)
        {
            if (tuple == null) return;

            var semanticModel = context.SemanticModel;
            if (semanticModel == null) return;

            var variables = tuple.Arguments.Select(argument => argument.GetFirstChild())
                                           .NotOfType<SyntaxNode, DeclarationExpressionSyntax>()
                                           .Select(node => node.GetNodeInfo(semanticModel));
            foreach ((var node, var symbol) in variables)
                ReportIfNecessary(symbol, node, context);
        } // private static void CheckTupleNode (TupleExpressionSyntax, SemanticModel)

        /// <summary>
        /// Analyzes <see cref="ArgumentSyntax"/>.
        /// </summary>
        /// <param name="context">Context to analyze.</param>
        private static void AnalyzeOutParameterNode(SyntaxNodeAnalysisContext context)
        {
            var semanticModel = context.SemanticModel;
            if (semanticModel == null) return;

            var node = (ArgumentSyntax)context.Node;
            if (!node.GetFirstToken().IsKind(SyntaxKind.OutKeyword)) return;  // only when containing `out` keyword
            if (node.ChildNodes().Contains(typeof(DeclarationExpressionSyntax))) return;  // exclude `out var`

            var variable = node.ChildNodes().LastOrDefault();
            if (variable == null) return;
            var symbol = semanticModel.GetSymbolInfo(variable).Symbol;
            ReportIfNecessary(symbol, node, context);
        } // private static void AnalyzeOutParameterNode (SyntaxNodeAnalysisContext)

        /// <summary>
        /// Checks if the reassignment is not allowed.
        /// </summary>
        /// <param name="symbol">The symbol to check.</param>
        /// <param name="node">The node from which the inspection originates.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns><c>true</c> if reassignment is not allowed; otherwise, <c>false</c></returns>
        public static bool CheckIfVariableIsNotReassignable(ISymbol? symbol, SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (symbol == null) return false;
            if (!symbol.IsPresumedNotReassignable()) return false;

            var name = symbol.Name;
            if (CheckMutableRulePatterns(node, semanticModel, name, cancellationToken)) return false;
            return true;
        } // public static bool CheckIfInvalidReassignment (ISymbol?, SyntaxNode, SemanticModel, CancellationToken)

        /// <summary>
        /// Reports diagnostic if necessary.
        /// </summary>
        /// <param name="symbol">The symbol to check.</param>
        /// <param name="node">The node from which the inspection originates.</param>
        /// <param name="context">Context to analyze.</param>
        private static void ReportIfNecessary(ISymbol? symbol, SyntaxNode node, SyntaxNodeAnalysisContext context)
        {
            if (!CheckIfVariableIsNotReassignable(symbol, node, context.SemanticModel, context.CancellationToken)) return;
            var name = symbol?.Name;
            context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation(), name));
        } // private static void ReportIfNecessary (ISymbol?, SyntaxNode, SyntaxNodeAnalysisContext)

        /// <summary>
        /// Checks to see if an attribute is set that allows reassignment to the variable.
        /// </summary>
        /// <param name="node">A node to start the inspection.</param>
        /// <param name="semanticModel">The semantic model to get symbol information.</param>
        /// <param name="name">The name of identifier to check.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns><c>true</c> if the variable is mutable; otherwise, <c>false</c>.</returns>
        private static bool CheckMutableRulePatterns(SyntaxNode node, SemanticModel semanticModel, string name, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IEnumerable<AttributeSyntax>? attributes = null;
            static IEnumerable<AttributeSyntax>? TryGetAttributes(SyntaxNode node)
            {
                if (node is MethodDeclarationSyntax methodDeclaration) return methodDeclaration.AttributeLists.GetAttributes();
                if (node is LocalFunctionStatementSyntax localFunction) return localFunction.AttributeLists.GetAttributes();
                return null;
            }

            if ((attributes = TryGetAttributes(node)) != null)
            {
                foreach (var attribute in attributes)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var symbol = semanticModel.GetSymbolInfo(attribute).Symbol;
                    var attrName = symbol?.GetFullyQualifiedName();
                    if (attrName != ReassignableVariableAttributeGenerator.ReassignableVariableAttributeName) continue;

                    var args = attribute.ArgumentList?.Arguments ?? Enumerable.Empty<AttributeArgumentSyntax>();
                    var names = args.Select(arg => arg.GetFirstChild())
                                    .Cast<LiteralExpressionSyntax>()
                                    .Where(expression => expression.IsKind(SyntaxKind.StringLiteralExpression))
                                    .Select(literal => literal.ToStringValue());

                    if (names.Contains(name)) return true;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            var parent = node.Parent;
            if (parent == null) return false;
            return CheckMutableRulePatterns(parent, semanticModel, name, cancellationToken);
        } // private static bool CheckMutableRulePatterns (SyntaxNode, SemanticModel, string, CancellationToken)
    } // public class ReadonlyLocalVariablesAnalyzer : DiagnosticAnalyzer
} // namespace ReadonlyLocalVariables
