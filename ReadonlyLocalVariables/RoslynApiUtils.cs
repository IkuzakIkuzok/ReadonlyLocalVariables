
// (c) 2022 Kazuki KOHZUKI

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
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

        /// <summary>
        /// Gets string value from string literal syntax.
        /// </summary>
        /// <param name="literal">The string literal.</param>
        /// <returns>The string value.</returns>
        /// <exception cref="ArgumentException">"The kind of '<paramref name="literal"/>' must be StringLiteralExpression.</exception>
        public static string GetStringValue(this LiteralExpressionSyntax literal)
        {
            if (!literal.IsKind(SyntaxKind.StringLiteralExpression))
                throw new ArgumentException("The kind of 'literal' must be StringLiteralExpression.");

            var text = literal.ToString();
            if (text.StartsWith("@"))
                text = text.Substring(1);  // Processing related to escape sequences appears to be unnecessary.
            return text.Trim('"');
        } // public static string GetStringValue (this LiteralExpressionSyntax)

        /// <summary>
        /// Returns a value indicating whether the symbol is a local variable.
        /// </summary>
        /// <param name="symbol">The symbol to check.</param>
        /// <returns><c>true</c> if <paramref name="symbol"/> is a local variable; otherwise, <c>false</c>.</returns>
        public static bool IsLocalVariable(this ISymbol symbol)
        {
            var declaringSyntax = symbol.OriginalDefinition.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
            if (declaringSyntax == null) return false;
            if (declaringSyntax.Parent?.Parent is FieldDeclarationSyntax) return false;
            if (declaringSyntax is PropertyDeclarationSyntax) return false;

            return true;
        } // public static bool CheckIfVariableIsLocal (this ISymbol)
    } // public static class RoslynApiUtils
} // namespace ReadonlyLocalVariables
