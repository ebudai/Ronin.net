// Copyright © 2026 Eric Budai

using System;
using System.Collections.Generic;
using System.Linq;
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

    /// <summary>
    ///     What makes two derivations the same derivation, which is their shape
    ///     and not their sentence.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     The cell used to identify a derivation by <see cref="ToString"/>,
    ///     with a comment that made it a claim: two derivations that read the
    ///     same way ARE the same reading. That is false for nested calls,
    ///     because <see cref="Call.Render"/> inserts its arguments without
    ///     delimiting itself — so «print(send(a, b))» and «print-to(send(a), b)»
    ///     both render «print send «a» to «b»», the second was discarded as a
    ///     duplicate of the first, and the resolver answered «Resolved» to a
    ///     statement with two meanings. Both are reachable by a bracket, so
    ///     nothing downstream could notice: the reading it erased was missing
    ///     from the count, from the witness, and from the property test that
    ///     asks the resolver what its readings are.
    ///     </para>
    ///     <para>
    ///     Identity by shape, presentation by <see cref="ToString"/>, and the
    ///     two no longer constrain each other — a rendering may be improved
    ///     without merging meanings, and two meanings stay two however alike
    ///     they read.
    ///     </para>
    ///     <para>
    ///     A pattern contributes its SHAPE rather than its declaration, so two
    ///     identical declarations of one pattern still collapse to one
    ///     derivation. That policy belongs to overloading and already lives
    ///     there; what does not belong is collapsing different trees.
    ///     </para>
    ///     <para>
    ///     A HASH and a comparison rather than a key string. The first version
    ///     built one length-prefixed string per node, which is correct and cost
    ///     14 MB on the measured statement — the allocation guard is there to
    ///     ask whether an identity needs to be materialised at all, and this one
    ///     does not. The hash is computed once per node and the comparison runs
    ///     only where hashes agree.
    ///     </para>
    /// </remarks>
    public static IEqualityComparer<Node> Same { get; } = new Shapes();

    /// <summary>
    ///     Whether this is the same derivation as another, by shape.
    /// </summary>
    ///
    /// <remarks>
    ///     Structural for a reason no test reaches: a hash is not an identity.
    ///     Every pair of trees a test can construct already lands in different
    ///     buckets, so comparing renderings here would pass the whole suite —
    ///     and would merge two meanings the moment their hashes collided, which
    ///     is the defect this replaced, arriving by a door nobody can open on
    ///     purpose. The guard is the pair: revert both this and
    ///     <see cref="Hash"/> to the rendering and the tie test and the repair
    ///     property both fail.
    /// </remarks>
    public abstract bool Alike(Node other);

    /// <summary>What this node is built from, for a walk that asks about a whole tree.</summary>
    protected virtual IReadOnlyList<Node> Within => [];

    /// <summary>Every node in this tree, this one included.</summary>
    public IEnumerable<Node> Whole => Within.SelectMany(part => part.Whole).Prepend(this);

    /// <summary>The shape's hash, cached: the tables ask once per offer and per lookup.</summary>
    protected int Shape => shape ??= Hash();

    protected abstract string Render();

    protected abstract int Hash();

    private string rendered;
    private int? shape;

    private sealed class Shapes : IEqualityComparer<Node>
    {
        public bool Equals(Node node, Node other) => ReferenceEquals(node, other) || node.Alike(other);

        public int GetHashCode(Node node) => node.Shape;
    }

    /// <summary>A literal, which denotes itself and costs no lookup.</summary>
    internal sealed class Literal(string text) : Node
    {
        public string Text { get; } = text;

        protected override string Render() => Text;

        public override bool Alike(Node other) => other is Literal literal && literal.Text == Text;

        protected override int Hash() => HashCode.Combine('l', Text);
    }

    /// <summary>A name in scope. One lookup.</summary>
    internal sealed class Name(string words) : Node
    {
        /// <summary>Space separated, exactly as it appears in <c>SymbolTable.Names</c>.</summary>
        public string Words { get; } = words;

        protected override string Render() => $"«{Words}»";

        public override bool Alike(Node other) => other is Name name && name.Words == Words;

        protected override int Hash() => HashCode.Combine('n', Words);
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

        // A DIFFERENT tag from a name, because they render alike and are not
        // alike: one introduces the words and the other reads them. Sharing the
        // tag would make a loop's variable indistinguishable from a reference to
        // something already in scope, which is the very confusion this node
        // exists to end.
        public override bool Alike(Node other) => other is Binding binding && binding.Words == Words;

        protected override int Hash() => HashCode.Combine('b', Words);
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

        public override bool Alike(Node other)
            => other is Group group && group.Collection == Collection && group.Parts.SequenceEqual(Parts, Same);

        protected override IReadOnlyList<Node> Within => Parts;

        protected override int Hash() => Parts.Aggregate(HashCode.Combine('g', Collection),
                                                         (hash, part) => HashCode.Combine(hash, part.Shape));
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

        public override bool Alike(Node other)
            => other is Operation operation
            && operation.Symbol == Symbol
            && ReferenceEquals(operation.Operator, Operator)
            && operation.Left.Alike(Left)
            && operation.Right.Alike(Right);

        protected override IReadOnlyList<Node> Within => [Left, Right];

        protected override int Hash() => HashCode.Combine('o', Symbol, Left.Shape, Right.Shape);
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

        public override bool Alike(Node other)
            => other is Previous previous && previous.Words == Words && previous.Argument.Alike(Argument);

        protected override IReadOnlyList<Node> Within => [Argument];

        protected override int Hash() => HashCode.Combine('p', Words, Argument.Shape);
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

        // The pattern by VALUE, so two identical declarations of one shape are
        // still one derivation. That collapsing is overloading's policy and it
        // already lives there; what does not belong is collapsing two trees.
        public override bool Alike(Node other)
            => other is Call call && call.Pattern.Equals(Pattern) && call.Arguments.SequenceEqual(Arguments, Same);

        protected override IReadOnlyList<Node> Within => Arguments;

        protected override int Hash() => Arguments.Aggregate(HashCode.Combine('c', Pattern),
                                                             (hash, argument) => HashCode.Combine(hash, argument.Shape));
    }
}
