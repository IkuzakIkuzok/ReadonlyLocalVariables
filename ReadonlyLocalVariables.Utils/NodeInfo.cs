
//(c) 2022 Kazuki KOHZUKI

using Microsoft.CodeAnalysis;

namespace ReadonlyLocalVariables
{
    /// <summary>
    /// Represents a syntax node and its correspond symbol.
    /// </summary>
    public readonly struct NodeInfo
    {
        /// <summary>
        /// Gets the node.
        /// </summary>
        public SyntaxNode Node { get; }

        /// <summary>
        /// Gets the symbol.
        /// </summary>
        public ISymbol? Symbol { get; }

        /// <summary>
        /// Gets the name of the symbol.
        /// </summary>
        public string? Name => this.Symbol?.Name;

        public NodeInfo(SyntaxNode node, ISymbol? symbol)
        {
            this.Node = node;
            this.Symbol = symbol;
        } // ctor (SyntaxNode, ISymbol?)

        public void Deconstruct(out SyntaxNode node, out ISymbol? symbol)
        {
            node = this.Node;
            symbol = this.Symbol;
        } // public void Deconstruct (out SyntaxNode, out ISymbol?)
    } // public readonly struct NodeInfo
} // namespace ReadonlyLocalVariables
