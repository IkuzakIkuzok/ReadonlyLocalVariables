
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
        /// Gets the first child node in prefix document order.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>The first child node of the <paramref name="node"/>.</returns>
        public static SyntaxNode GetFirstChild(this SyntaxNode node)
            => node.ChildNodes().First();

        /// <summary>
        /// Gets attributes from ttribute lists.
        /// </summary>
        /// <param name="attributeLists">The attribute lists.</param>
        /// <returns>An <see cref="IEnumerable{AttributeSyntax}"/> containing attributes from <paramref name="attributeLists"/>.</returns>
        public static IEnumerable<AttributeSyntax> GetAttributes(this SyntaxList<AttributeListSyntax> attributeLists)
            => attributeLists.SelectMany(list => list.Attributes);

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
        public static string ToStringValue(this LiteralExpressionSyntax literal)
        {
            if (!literal.IsKind(SyntaxKind.StringLiteralExpression))
                throw new ArgumentException("The kind of 'literal' must be StringLiteralExpression.");

            var text = literal.ToString();
            if (text.StartsWith("@"))
                text = text.Substring(1);  // Processing related to escape sequences appears to be unnecessary.
            return text.Trim('"');
        } // public static string ToStringValue (this LiteralExpressionSyntax)

        /// <summary>
        /// Gets declaring syntax of a symbol.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>The declaring syntax.</returns>
        public static SyntaxNode? GetDeclaringSyntax(this ISymbol symbol)
            => symbol.OriginalDefinition.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();

        /// <summary>
        /// Returns a value indicating whether the symbol is a local variable.
        /// </summary>
        /// <param name="symbol">The symbol to check.</param>
        /// <returns><c>true</c> if <paramref name="symbol"/> is a local variable; otherwise, <c>false</c>.</returns>
        public static bool IsLocalVariable(this ISymbol symbol)
        {
            var declaringSyntax = symbol.GetDeclaringSyntax();
            if (declaringSyntax == null) return false;
            if (declaringSyntax.Parent?.Parent is FieldDeclarationSyntax) return false;
            if (declaringSyntax is PropertyDeclarationSyntax) return false;
            if (declaringSyntax is ParameterSyntax) return false;

            return declaringSyntax.Parent is VariableDeclarationSyntax;
        } // public static bool CheckIfVariableIsLocal (this ISymbol)

        /// <summary>
        /// Returns a value indicating whether the symbol is a parameter.
        /// </summary>
        /// <param name="symbol">The symbol to check.</param>
        /// <param name="isOutParameter">When this method returns, contains <c>true</c>
        /// if <paramref name="isOutParameter"/> is an parameter with <c>out</c> modifier, or <c>false</c> if not.
        /// This parameter is passed uninitialized; any value originally supplied in <paramref name="isOutParameter"/> will be overwritten.</param>
        /// <returns><c>true</c> if <paramref name="symbol"/> is a parameter; otherwise, <c>false</c>.</returns>
        public static bool IsParameter(this ISymbol symbol, out bool isOutParameter)
        {
            isOutParameter = false;

            var declaringSyntax = symbol.GetDeclaringSyntax();
            if (declaringSyntax == null) return false;

            if (declaringSyntax is not ParameterSyntax) return false;
            isOutParameter = declaringSyntax.ChildTokens().Where(token => token.IsKind(SyntaxKind.OutKeyword)).Any();
            return true;
        } // public static bool IsParameter (this ISymbol, out bool)

        /// <summary>
        /// Determines whether the variable is presumed to be not reassignable.
        /// </summary>
        /// <param name="symbol">The symbol of the variable to check.</param>
        /// <returns><c>true</c> if the variable is a local variable or a parameter without <c>out</c> modifier;
        /// otherwise, <c>false</c>.</returns>
        public static bool IsPresumedNotReassignable(this ISymbol symbol)
        {
            if (symbol.IsLocalVariable()) return true;
            if (symbol.IsParameter(out var isOut)) return !isOut;
            return false;
        } // public static bool IsPresumedNotReassignable (this ISymbol)

        /// <summary>
        /// Returns a literal expression that represents the current instance.
        /// </summary>
        /// <param name="s">The string value.</param>
        /// <returns>The string literal expression.</returns>
        public static LiteralExpressionSyntax ToLiteralExpression(this string s)
            => SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(s));

        /// <summary>
        /// Creates a <see cref="SeparatedSyntaxList{TNode}"/> from an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="TNode">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to create a <see cref="SeparatedSyntaxList{TNode}"/> from.</param>
        /// <returns></returns>
        public static SeparatedSyntaxList<TNode> ToSeparatedSyntaxList<TNode>(this IEnumerable<TNode> source) where TNode : SyntaxNode
            => new SeparatedSyntaxList<TNode>().AddRange(source);

        /// <summary>
        /// Gets <see cref="NodeInfo"/> of a syntax node.
        /// </summary>
        /// <param name="node">The syntax node.</param>
        /// <param name="semanticModel">The semantic model to get symbol info.</param>
        /// <returns>The instance of the <see cref="NodeInfo"/>.</returns>
        public static NodeInfo GetNodeInfo(this SyntaxNode node, SemanticModel semanticModel)
            => new(node, semanticModel.GetSymbolInfo(node).Symbol);
    } // public static class RoslynApiUtils
} // namespace ReadonlyLocalVariables
