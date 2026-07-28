// Copyright © 2026 Eric Budai

using System.Collections.Generic;
using System.Text;

namespace Ronin.Compiler;

/// <summary>
///     What a statement was resolved to mean. One node per decision the
///     <see cref="Resolver"/> committed to.
/// </summary>
///
/// <remarks>
///     <para>
///     The resolver scores spans, and a span's score used to be carried by the
///     rendered string alone — «(«base price» + «tax»)» told you the answer but
///     could only be read by a person. The tree is the answer; the string is now
///     a rendering of it, produced on demand and cached, and it is byte for byte
///     what it was before so the transcribed expectations still pin the same
///     readings.
///     </para>
///     <para>
///     Rendering is cached because a node is asked for its text once per
///     <c>Cell.Offer</c> that stores it and once more for every parent that
///     renders around it, and the tables offer the same subtree many times over.
///     </para>
/// </remarks>
internal abstract class Node
{
    public sealed override string ToString() => rendered ??= Render();

    protected abstract string Render();

    private string rendered;

    /// <summary>A literal, which denotes itself and costs no lookup.</summary>
    internal sealed class Literal(string text) : Node
    {
        public string Text { get; } = text;

        protected override string Render() => Text;
    }

    /// <summary>A name in scope. One lookup.</summary>
    internal sealed class Name(string words) : Node
    {
        /// <summary>Space separated, exactly as it appears in <c>SymbolTable.Names</c>.</summary>
        public string Words { get; } = words;

        protected override string Render() => $"«{Words}»";
    }

    /// <summary>A bracketed substatement. One lookup however large it is.</summary>
    internal sealed class Group(Node inner) : Node
    {
        public Node Inner { get; } = inner;

        protected override string Render() => $"⟨{Inner}⟩";
    }

    /// <summary>An operator applied to two operands. Free: no table is consulted.</summary>
    internal sealed class Operation(Node left, string symbol, Node right) : Node
    {
        public Node Left { get; } = left;
        public string Symbol { get; } = symbol;
        public Node Right { get; } = right;

        protected override string Render() => $"({Left} {Symbol} {Right})";
    }

    /// <summary>
    ///     A word pattern applied to its arguments, in hole order. One lookup.
    /// </summary>
    internal sealed class Call(Pattern pattern, IReadOnlyList<Node> arguments) : Node
    {
        public Pattern Pattern { get; } = pattern;

        /// <summary>One per hole in <see cref="Pattern"/>, left to right.</summary>
        public IReadOnlyList<Node> Arguments { get; } = arguments;

        protected override string Render()
        {
            StringBuilder rendering = new();
            var next = 0;

            foreach (var segment in Pattern.Segments)
            {
                if (rendering.Length is not 0) rendering.Append(' ');
                rendering.Append(segment ?? Arguments[next++].ToString());
            }

            return rendering.ToString();
        }
    }
}
