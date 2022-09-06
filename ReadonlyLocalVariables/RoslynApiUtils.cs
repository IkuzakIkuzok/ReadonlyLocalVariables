
// (c) 2022 Kazuki KOHZUKI

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace ReadonlyLocalVariables
{
    /// <summary>
    /// Provides utilities related to the Roslyn API.
    /// </summary>
    public static class RoslynApiUtils
    {
        /// <summary>
        /// Obtains the fully qualified name of the class.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>The fully qualified name.</returns>
        /// <remarks><see cref="ISymbol.ToDisplayParts(SymbolDisplayFormat?)"/> with argument <see cref="SymbolDisplayFormat.FullyQualifiedFormat"/>
        /// does NOT work as expected. This problem is already reported on <a href="https://github.com/dotnet/roslyn/issues/50259">GitHub</a>.</remarks>
        public static string GetFullyQualifiedName(this ISymbol symbol)
        {
            var definition = symbol.OriginalDefinition.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
            if (definition == null) return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            var node = definition;
            var names = new List<string>();
            while ((node = node.Parent) != null)
            {
                if (node is NamespaceDeclarationSyntax ns)
                    names.Add(ns.Name.ToString());
                if (node is ClassDeclarationSyntax @class)
                    names.Add(@class.ChildTokens().Where(token => token.IsKind(SyntaxKind.IdentifierToken)).First().Text);
            }
            names.Reverse();
            return string.Join(".", names);
        } // public static string GetFullyQualifiedName (this ISymbol)
    } // public static class RoslynApiUtils
} // namespace ReadonlyLocalVariables
