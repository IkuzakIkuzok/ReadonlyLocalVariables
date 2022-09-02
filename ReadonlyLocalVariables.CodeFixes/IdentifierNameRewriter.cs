
// (c) 2022 Kazuki KOHZUKI

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadonlyLocalVariables
{
    internal sealed class IdentifierNameRewriter : CSharpSyntaxRewriter
    {
        private readonly int position;
        private readonly string oldName, newName;

        internal IdentifierNameRewriter(int position, string oldName, string newName)
        {
            this.position = position;
            this.oldName = oldName;
            this.newName = newName;
        } // ctor (int, string, string)

        override public SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (node.SpanStart < this.position) return node;
            var oldToken = node.GetFirstToken();
            if (oldToken.ToString() != this.oldName) return node;
            var newToken = SyntaxFactory.Identifier(newName);
            return node.ReplaceToken(oldToken, newToken);
        } // override public SyntaxNode VisitIdentifierName (IdentifierNameSyntax)
    } // internal sealed class IdentifierNameRewriter : CSharpSyntaxRewriter
} // namespace ReadonlyLocalVariables
