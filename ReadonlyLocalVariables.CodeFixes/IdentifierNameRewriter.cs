
// (c) 2022 Kazuki KOHZUKI

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace ReadonlyLocalVariables
{
    /// <summary>
    /// Rewrites identifier names.
    /// </summary>
    internal sealed class IdentifierNameRewriter : CSharpSyntaxRewriter
    {
        private readonly int position;
        private readonly string oldName, newName;

        /// <summary>
        /// Initializes a new instance of the <see cref="IdentifierNameRewriter"/> class.
        /// </summary>
        /// <param name="position">Position at which the rewriting starts.</param>
        /// <param name="oldName">The old identifier name.</param>
        /// <param name="newName">The new identifier name.</param>
        internal IdentifierNameRewriter(int position, string oldName, string newName)
        {
            this.position = position;
            this.oldName = oldName;
            this.newName = newName;
        } // ctor (int, string, string)

        override public SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            /*
             * If the identifiers of class members and local variables conflict, 
             * access to class members must be qualified by `this` or `base`. 
             * In this case, the first child node of the parent node of IdentifierNameSyntax
             * does not match the original IdentifierNameSyntax (it must be a qualifier such as `this`).
             * The definition should be verified using SemanticModel,
             * but since errors occur as nodes are rewritten, this method is used for simplicity.
             */
            if (node.Parent.ChildNodes().First() != node) return node;

            if (node.SpanStart < this.position) return node;
            var oldToken = node.GetFirstToken();
            if (oldToken.ToString() != this.oldName) return node;
            var newToken = SyntaxFactory.Identifier(newName);
            return node.ReplaceToken(oldToken, newToken.WithTriviaFrom(oldToken));
        } // override public SyntaxNode VisitIdentifierName (IdentifierNameSyntax)
    } // internal sealed class IdentifierNameRewriter : CSharpSyntaxRewriter
} // namespace ReadonlyLocalVariables
