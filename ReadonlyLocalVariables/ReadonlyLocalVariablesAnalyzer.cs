﻿
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

namespace ReadonlyLocalVariables
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ReadonlyLocalVariablesAnalyzer : DiagnosticAnalyzer
    {
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

            context.RegisterSyntaxNodeAction(AnalyzeNode,
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
        } // override public void Initialize (AnalysisContext)

        private static async void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var semanticModel = context.SemanticModel;
            if (semanticModel == null) return;

            var node = (AssignmentExpressionSyntax)context.Node;
            if (node.Parent is ForStatementSyntax) return;
            var leftNode = node.Left;
            var leftSymbol = semanticModel.GetSymbolInfo(leftNode).Symbol;
            if (leftSymbol == null) return;
            var declaringSyntax = leftSymbol.OriginalDefinition.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
            if (!CheckIfDeclarationIsLocal(declaringSyntax)) return;
            var name = leftSymbol.Name ?? string.Empty;
            if (await CheckMutableRulePatterns(node, name, context.CancellationToken)) return;
            context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation(), name));
        } // private static void AnalyzeNode (SyntaxNodeAnalysisContext)

        private static bool CheckIfDeclarationIsLocal(SyntaxNode? declaringSyntax)
        {
            if (declaringSyntax?.Parent?.Parent is MemberDeclarationSyntax) return false;

            return true;
        } // private static bool CheckIfDeclarationIsLocal (SyntaxNode?)

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
