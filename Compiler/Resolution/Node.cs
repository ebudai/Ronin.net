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

    /// <summary>
    ///     A name being DECLARED here, which is not a name being read.
    /// </summary>
    ///
    /// <remarks>
    ///     The loop's variable, and the reason it needs a shape of its own: the
    ///     resolver worked out that this occurrence declares rather than refers —
    ///     that is what lets «for each bank in banks» resolve against a scope
    ///     where «bank» does not exist yet — and then handed back a
    ///     <see cref="Name"/>, whose whole contract is "in scope, one lookup".
    ///     Evaluating the tree read the name the loop was about to introduce and
    ///     reported it undeclared. Knowing something and then erasing it is worse
    ///     than never knowing it, because everything downstream looks right.
    ///     <para>
    ///     It renders as a name, because that is how a reader wrote it.
    ///     </para>
    /// </remarks>
    internal sealed class Binding(string words) : Node
    {
        /// <summary>Space separated, as the scope it declares into will hold it.</summary>
        public string Words { get; } = words;

        protected override string Render() => $"«{Words}»";
    }

    /// <summary>
    ///     A bracketed substatement. One lookup however large it is, and one part
    ///     unless separators divided it — «(x, y)» is a group of two, which is how
    ///     a parameter block of two receives its arguments.
    /// </summary>
    internal sealed class Group(IReadOnlyList<Node> parts, bool collection = false) : Node
    {
        /// <summary>
        ///     Whether the brackets were a COLLECTION rather than grouping.
        /// </summary>
        ///
        /// <remarks>
        ///     «(x)» is one value in brackets and «[x]» is a list of one, and the
        ///     resolver saw the same node for both — so a singleton list
        ///     evaluated to its element and «[10] @ 1» reported that its left
        ///     operand was not a list. A two-element list worked by accident,
        ///     because more than one part had nowhere to collapse to.
        /// </remarks>
        public bool Collection { get; } = collection;

        /// <remarks>
        ///     Copied: a node caches its rendering, so a caller still holding the
        ///     list it passed could change what the node contains without
        ///     changing what it says it contains.
        /// </remarks>
        public IReadOnlyList<Node> Parts { get; } = [.. parts];

        protected override string Render()
            => Collection ? $"[{string.Join(", ", Parts)}]" : $"⟨{string.Join(", ", Parts)}⟩";
    }

    /// <summary>An operator applied to two operands. Free: no table is consulted.</summary>
    ///
    /// <remarks>
    ///     Carries the <see cref="Operator"/> the resolver chose, not just its
    ///     symbol. Storing the symbol alone meant evaluation looked it up again in
    ///     a different registry — so resolution could accept an operator the
    ///     evaluator had never heard of, and an implementation the scope had
    ///     replaced was ignored in favour of the built-in one. Two tables wearing
    ///     the same name.
    /// </remarks>
    internal sealed class Operation(Node left, string symbol, Operator op, Node right) : Node
    {
        public Node Left { get; } = left;
        public string Symbol { get; } = symbol;
        public Operator Operator { get; } = op;
        public Node Right { get; } = right;

        protected override string Render() => $"({Left} {Symbol} {Right})";
    }

    /// <summary>
    ///     The previous value of one reactive name. Unlike a call argument, the
    ///     reference is not evaluated first: doing so would record a dependency
    ///     on the current value and turn a self-reference through «old» into a
    ///     cycle. The resolver has already proved that <see cref="Argument"/> is
    ///     only that name, optionally grouped.
    /// </summary>
    internal sealed class Previous(string name, Node argument) : Node
    {
        public string Words { get; } = name;
        public Node Argument { get; } = argument;

        protected override string Render() => $"{SymbolTable.Old} {Argument}";
    }

    /// <summary>
    ///     A word pattern applied to its arguments, in hole order. One lookup.
    /// </summary>
    internal sealed class Call(Pattern pattern, IReadOnlyList<Node> arguments) : Node
    {
        public Pattern Pattern { get; } = pattern;

        /// <summary>One per hole in <see cref="Pattern"/>, left to right.</summary>
        /// <remarks>Copied, for the reason <see cref="Group.Parts"/> is.</remarks>
        public IReadOnlyList<Node> Arguments { get; } = [.. arguments];

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
