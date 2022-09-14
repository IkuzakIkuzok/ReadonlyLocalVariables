
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
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnnecessaryAttributesDetector : DiagnosticAnalyzer
    {
        public static readonly string PermissionDiagnosticId = "RO3001";
        public static readonly string AttributeDiagnosticId = "RO3002";

        private static LocalizableResourceString GetLocalizableResourceString(string resourceName)
            => new(resourceName, AnalyzerResources.ResourceManager, typeof(AnalyzerResources));

        private static readonly LocalizableResourceString permissionTitle = GetLocalizableResourceString(nameof(AnalyzerResources.UnnecessaryPermissionTitle));
        private static readonly LocalizableResourceString permissionMessageFormat = GetLocalizableResourceString(nameof(AnalyzerResources.UnnecessaryPermissionMessageFormat));

        private static readonly LocalizableResourceString attributeTitle = GetLocalizableResourceString(nameof(AnalyzerResources.UnnecessaryAttributeTitle));
        private static readonly LocalizableResourceString attributeMessageFormat = GetLocalizableResourceString(nameof(AnalyzerResources.UnnecessaryAttributeMessageFormat));

        private const string Category = "CodeStyle";

        private static readonly DiagnosticDescriptor PermissionRule = new(
            id                : PermissionDiagnosticId,
            title             : permissionTitle,
            messageFormat     : permissionMessageFormat,
            category          : Category,
            defaultSeverity   : DiagnosticSeverity.Info,
            isEnabledByDefault: true
        );

        private static readonly DiagnosticDescriptor AttributeRule = new(
            id                : AttributeDiagnosticId,
            title             : attributeTitle,
            messageFormat     : attributeMessageFormat,
            category          : Category,
            defaultSeverity   : DiagnosticSeverity.Info,
            isEnabledByDefault: true
        );

        override public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(PermissionRule, AttributeRule);

        override public void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
        } // override public void Initialize (AnalysisContext)

        private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
        {
            var semanticModel = context.SemanticModel;

            var attribute = (AttributeSyntax)context.Node;
            var attrSymbol = semanticModel.GetSymbolInfo(attribute, context.CancellationToken).Symbol;
            var attrName = attrSymbol?.GetFullyQualifiedName();
            if (attrName != ReassignableVariableAttributeGenerator.ReassignableVariableAttributeName) return;

            var args = attribute.ArgumentList?.Arguments ?? Enumerable.Empty<AttributeArgumentSyntax>();
            if (!args.Any())
            {
                context.ReportDiagnostic(Diagnostic.Create(AttributeRule, attribute.GetLocation(), attribute.Name));
                return;
            }
            var names = args.Select(arg => arg.GetFirstChild())
                            .Cast<LiteralExpressionSyntax>()
                            .Where(expression => expression.IsKind(SyntaxKind.StringLiteralExpression))
                            .ToList();

            var method = attribute.Parent?.Parent;
            if (method is not MethodDeclarationSyntax && method is not LocalFunctionStatementSyntax) return;

            var assignedVariavbles = GetAssignedLocalVariables(method, semanticModel, context.CancellationToken);
            var unnecessaryArgs = names.Where(arg => !assignedVariavbles.Contains(arg.ToStringValue())).ToList();

            if (unnecessaryArgs.Count == names.Count)
            {
                context.ReportDiagnostic(Diagnostic.Create(AttributeRule, attribute.GetLocation(), attribute.Name));
            }
            else
            {
                foreach (var arg in unnecessaryArgs)
                {
                    var name = arg.ToStringValue();
                    context.ReportDiagnostic(Diagnostic.Create(PermissionRule, arg.GetLocation(), name));
                }
            }
        } // private static void AnalyzeAttribute (SyntaxNodeAnalysisContext)

        private static IEnumerable<string> GetAssignedLocalVariables(SyntaxNode method, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var nodes = method.DescendantNodes();
            var assignments = nodes.OfType<AssignmentExpressionSyntax>();

            var assignmentLefts = assignments.Select(assignment => assignment.Left)
                                             .NotOfType<ExpressionSyntax, TupleExpressionSyntax>();

            var tupleElements = assignments.Select(assignment => assignment.Left as TupleExpressionSyntax)
                                           .Where(tuple => tuple != null)
#pragma warning disable CS8602
                                           .Select(tuple => tuple.Arguments)
#pragma warning restore
                                           .SelectMany(arguments => arguments.Select(argument => argument.GetFirstChild()))
                                           .NotOfType<SyntaxNode, DeclarationExpressionSyntax>();

            var arguments = nodes.OfType<ArgumentSyntax>();
            var outArguments = arguments.Where(node => node.GetFirstToken().IsKind(SyntaxKind.OutKeyword))
                                        .Select(node => node.ChildNodes())
                                        .Where(nodes => !nodes.Contains(typeof(DeclarationExpressionSyntax)))
                                        .Select(nodes => nodes.Last());

            var variables = assignmentLefts.Concat(tupleElements).Concat(outArguments)
                                           .Select(node => semanticModel.GetSymbolInfo(node, cancellationToken).Symbol)
                                           .Where(symbol => symbol?.IsPresumedNotReassignable() ?? false)
                                           .Select(symbol => symbol?.Name ?? string.Empty);
            return variables;
        } // private static IEnumerable<string> GetAssignedLocalVariables (SyntaxNode, SemanticModel, CancellationToken)
    } // public class UnnecessaryAttributesDetector : DiagnosticAnalyzer
} // namespace ReadonlyLocalVariables
