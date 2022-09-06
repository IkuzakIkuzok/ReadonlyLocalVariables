
// (c) 2022 Kazuki KOHZUKI

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

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
        private static async void AnalyzeAssignmentNode(SyntaxNodeAnalysisContext context)
        {
            var semanticModel = context.SemanticModel;
            if (semanticModel == null) return;

            var node = (AssignmentExpressionSyntax)context.Node;
            if (node.Parent is ForStatementSyntax) return;
            var leftNode = node.Left;
            if (leftNode is TupleExpressionSyntax tuple)
            {
                await CheckTupleNode(tuple, context);
                return;
            }
            var leftSymbol = semanticModel.GetSymbolInfo(leftNode).Symbol;
            await ReportIfNecessary(leftSymbol, node, context);
        } // private static void AnalyzeAssignmentNode (SyntaxNodeAnalysisContext)

        private static async Task CheckTupleNode(TupleExpressionSyntax tuple, SyntaxNodeAnalysisContext context)
        {
            if (tuple == null) return;

            var semanticModel = context.SemanticModel;
            if (semanticModel == null) return;

            var variables = tuple.Arguments.Select(argument => argument.ChildNodes().First())
                                           .Where(node => node is not DeclarationExpressionSyntax)
                                           .Select(node => (node, semanticModel.GetSymbolInfo(node).Symbol));
            foreach ((var node, var symbol) in variables)
                await ReportIfNecessary(symbol, node, context);
        } // private static async void CheckTupleNode (TupleExpressionSyntax, SemanticModel)

        /// <summary>
        /// Analyzes <see cref="ArgumentSyntax"/>.
        /// </summary>
        /// <param name="context">Context to analyze.</param>
        private static async void AnalyzeOutParameterNode(SyntaxNodeAnalysisContext context)
        {
            var semanticModel = context.SemanticModel;
            if (semanticModel == null) return;

            var node = (ArgumentSyntax)context.Node;
            if (node.GetFirstToken().Kind() != SyntaxKind.OutKeyword) return;  // only when containing `out` keyword
            if (node.ChildNodes().OfType<DeclarationExpressionSyntax>().Any()) return;  // exclude `out var`

            var variable = node.ChildNodes().Last();
            if (variable == null) return;
            var symbol = semanticModel.GetSymbolInfo(variable).Symbol;
            await ReportIfNecessary(symbol, node, context);
        } // private static async void AnalyzeOutParameterNode (SyntaxNodeAnalysisContext)

        /// <summary>
        /// Checks if the reassignment is not allowed.
        /// </summary>
        /// <param name="symbol">The symbol to check.</param>
        /// <param name="node">The node from which the inspection originates.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns><c>true</c> if reassignment is not allowed; otherwise, <c>false</c></returns>
        public static async Task<bool> CheckIfVariableIsNotReassignable(ISymbol? symbol, SyntaxNode node, CancellationToken cancellationToken)
        {
            if (symbol == null) return false;
            var declaringSyntax = symbol.OriginalDefinition.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
            if (!CheckIfDeclarationIsLocal(declaringSyntax)) return false;
            var name = symbol.Name;
            if (await CheckMutableRulePatterns(node, name, cancellationToken)) return false;
            return true;
        } // public static async Task<bool> CheckIfInvalidReassignment (ISymbol?, SyntaxNode, CancellationToken)

        /// <summary>
        /// Reports diagnostic if necessary.
        /// </summary>
        /// <param name="symbol">The symbol to check.</param>
        /// <param name="node">The node from which the inspection originates.</param>
        /// <param name="context">Context to analyze.</param>
        /// <returns>The <see cref="Task"/> object representing the asynchronous operation.</returns>
        private static async Task ReportIfNecessary(ISymbol? symbol, SyntaxNode node, SyntaxNodeAnalysisContext context)
        {
            if (!await CheckIfVariableIsNotReassignable(symbol, node, context.CancellationToken)) return;
            var name = symbol?.Name;
            context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation(), name));
        } // private static async Task ReportIfNecessary (ISymbol?, SyntaxNode, SyntaxNodeAnalysisContext)

        /// <summary>
        /// Checks if the variable declaration is a local variable declaration.
        /// </summary>
        /// <param name="declaringSyntax">The declaration syntax to check.</param>
        /// <returns><c>true</c> if the declaration is a local variable declaration;
        /// otherwise, <c>false</c></returns>
        private static bool CheckIfDeclarationIsLocal(SyntaxNode? declaringSyntax)
        {
            if (declaringSyntax == null) return false;
            if (declaringSyntax.Parent?.Parent is FieldDeclarationSyntax) return false;
            if (declaringSyntax is PropertyDeclarationSyntax) return false;

            return true;
        } // private static bool CheckIfDeclarationIsLocal (SyntaxNode?)

        /// <summary>
        /// Checks to see if an attribute is set that allows reassignment to the variable.
        /// </summary>
        /// <param name="node">A node to start the inspection.</param>
        /// <param name="name">The name of identifier to check.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The <see cref="Task"/> object representing the asynchronous operation.</returns>
        private static async Task<bool> CheckMutableRulePatterns(SyntaxNode node, string name, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IEnumerable<AttributeSyntax>? attributes = null;
            static IEnumerable<AttributeSyntax>? TryGetAttributes(SyntaxNode node)
            {
                if (node is MethodDeclarationSyntax methodDeclaration) return methodDeclaration.AttributeLists.SelectMany(list => list.Attributes);
                if (node is LocalFunctionStatementSyntax localFunction) return localFunction.AttributeLists.SelectMany(list => list.Attributes);
                return null;
            }

            if ((attributes = TryGetAttributes(node)) != null)
            {
                foreach (var attribute in attributes)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var attrName = attribute.Name.ToFullString();
                    if (!attrName.EndsWith("Attribute")) attrName += "Attribute";
                    if (attrName != "ReassignableVariableAttribute") continue;
                    var args = attribute.ArgumentList?.Arguments ?? Enumerable.Empty<AttributeArgumentSyntax>();
                    var names = args.Select(arg => arg.ToFullString()?.Trim('"'));
                    if (names.Any(n => n == name)) return true;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            var parent = node.Parent;
            if (parent == null) return false;
            return await CheckMutableRulePatterns(parent, name, cancellationToken);
        } // private static async Task<bool> CheckMutableRulePatterns (SyntaxNode, string, CancellationToken)
    } // public class ReadonlyLocalVariablesAnalyzer : DiagnosticAnalyzer
} // namespace ReadonlyLocalVariables
