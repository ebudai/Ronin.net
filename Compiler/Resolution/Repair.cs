// Copyright © 2026 Eric Budai

using System.Collections.Generic;
using System.Linq;

namespace Ronin.Compiler;

/// <summary>One bracket, and where it goes.</summary>
///
/// <param name="At">An offset in the source the statement was lexed from.</param>
internal readonly record struct Insertion(int At, string Text);

/// <summary>
///     A reading, and the edit that selects it.
/// </summary>
///
/// <remarks>
///     <para>
///     A message cannot be clicked. The design asked for the bracketings to be
///     IN the error and selectable, which means edits with positions rather than
///     a sentence describing where a bracket would go — an editor applies the
///     first and can only print the second.
///     </para>
///     <para>
///     <see cref="Rank"/> is the order to offer them in, cheapest first, and is
///     the whole of what cost does now. It may order the suggestions and it may
///     never choose among them: the moment it chooses, every silent capture this
///     design removed comes back looking like a feature.
///     </para>
/// </remarks>
internal sealed class Repair
{
    public Repair(string reading, int rank, IReadOnlyList<Insertion> insertions)
    {
        Reading = reading;
        Rank = rank;
        Insertions = Owned.Copy(insertions);
    }

    public string Reading { get; }

    public int Rank { get; }

    /// <summary>The brackets to type, in the order to apply them.</summary>
    ///
    /// <remarks>
    ///     Left to right by offset, and where two land at one offset — a call's
    ///     bracket and its argument's closing together — the outer before the
    ///     inner, so applying them in this order nests «(send (a to b))» rather
    ///     than crossing it. An editor applies same-position edits in array order,
    ///     which is this one.
    ///     <para>
    ///     Owned, because positional would have made the caller's list a public
    ///     promise of its own beside the owned one — the same value handed out
    ///     twice with only one of them safe.
    ///     </para>
    /// </remarks>
    public IReadOnlyList<Insertion> Insertions { get; }
}

/// <summary>
///     The bracketings that select each reading of an ambiguous statement.
/// </summary>
///
/// <remarks>
///     <para>
///     FROM THE TREE, not from every subspan there is. A repair brackets a
///     subtree — so the spans worth trying are the spans the reading's own nodes
///     occupy, which a node knows because it carries its extent. Trying every
///     «(from, width)» instead was «O(n²)» candidates, each a full re-resolve of
///     the statement, and a twenty-word ambiguity took seconds. The nodes of a
///     reading are «O(n)», so this is «O(n)» candidates for a single bracket.
///     </para>
///     <para>
///     A SET of any size, not a fixed number of pairs. Some readings choose a
///     meaning for several children at once — «(send a to b) + (send a to b) +
///     (send a to b)» has a reading for each way of reading each third — and no
///     one bracket, nor any fixed number of them, selects it. So the search
///     brackets every one of the reading's subtrees at once, which pins the whole
///     structure, and then takes a bracket away wherever the reading survives
///     without it. Searching one pair, then exactly two, published an empty
///     repair for the first reading that needed three.
///     </para>
///     <para>
///     By RESOLVING each candidate rather than reasoning about the tree. The
///     claim a repair makes is "type this and the ambiguity is gone", so the
///     honest way to produce one is to type it and look. Each distinct candidate
///     is resolved once and cached, because two readings ask about the same
///     brackets.
///     </para>
///     <para>
///     PROPORTIONAL to the reading. The full bracketing verifies once and each
///     bracket is tried for removal once, which is «O(nodes)» resolutions.
///     Enumerating subsets of the spans by increasing size instead reached a set
///     that pins N children only past every smaller set that does not — «O(2ⁿ)»,
///     so an eight-child expression of fifty-five lexemes spent nine seconds and
///     eleven gigabytes and still found nothing, its answer's cardinality past
///     where the budget gave out. Past that budget of resolutions the trim stops
///     and the reading keeps a fuller repair, or an unverified reading is
///     reported without one — honest, where a hang is not.
///     </para>
/// </remarks>
internal static class Repairs
{
    /// <summary>How many candidate resolutions a statement's repairs may cost.</summary>
    private const int Budget = 4000;

    public static IReadOnlyList<Repair> For(Resolver resolver, IReadOnlyList<Lexeme> lexemes, Resolution ambiguity)
        => For(resolver, lexemes, ambiguity, Budget);

    /// <summary>
    ///     The repairs, spending at most <paramref name="budget"/> candidate
    ///     resolutions.
    /// </summary>
    ///
    /// <remarks>
    ///     The budget is a parameter only so a test can set it low enough to
    ///     reach the guard deterministically — the pathological statement that
    ///     spends four thousand resolutions is near the lexeme limit and slow to
    ///     build, and the behaviour past the budget is what matters: the reading
    ///     is reported and the repair is absent, never a hang.
    /// </remarks>
    internal static IReadOnlyList<Repair> For(Resolver resolver,
                                              IReadOnlyList<Lexeme> lexemes,
                                              Resolution ambiguity,
                                              int budget)
    {
        Search search = new(resolver, lexemes, budget);
        List<Repair> found = [];

        foreach (var alternative in ambiguity.Alternatives)
        {
            // NOT PUBLISHED when there is none. A repair with no insertions looks
            // selectable in an editor and does nothing when selected, which is
            // worse than an error that offers nothing — the second says where you
            // are and the first lies about it.
            if (search.Selecting(alternative) is not IReadOnlyList<Insertion> insertions) continue;

            found.Add(new Repair(alternative.ToString(), found.Count, insertions));
        }

        // Owned on the way out, like every other value this compiler hands over:
        // what an editor is about to apply should not change under it because the
        // thing that built it kept a reference.
        return Owned.Copy(found);
    }

    /// <summary>One statement's repair search, with its cache and budget.</summary>
    private sealed class Search(Resolver resolver, IReadOnlyList<Lexeme> lexemes, int budget)
    {
        private readonly Dictionary<string, Node> resolved = [];
        private int spent;

        /// <summary>
        ///     A small set of brackets that leaves only this reading.
        /// </summary>
        ///
        /// <remarks>
        ///     <para>
        ///     DERIVED from the tree, not searched for among its spans. Bracketing
        ///     every one of the target's own subtrees pins the whole structure, so
        ///     that one candidate selects the target by construction — and then a
        ///     bracket comes off wherever the reading survives without it, until
        ///     what is left is a set no member of which is redundant. A reading is
        ///     the whole statement's tree, and «Same» unwraps the brackets the
        ///     repair itself added when it checks.
        ///     </para>
        ///     <para>
        ///     PROPORTIONAL to the reading. Resolving one candidate per subtree —
        ///     the full set once, then one trial per bracket removed — is O(nodes)
        ///     resolutions. Trying every subset of the spans by increasing size
        ///     instead reached a set that pins N children only after resolving all
        ///     the smaller sets that do not, which is «O(2ⁿ)»: an eight-child
        ///     expression of fifty-five lexemes spent nine seconds and eleven
        ///     gigabytes and still found nothing, because the answer's cardinality
        ///     was past where the budget gave out.
        ///     </para>
        /// </remarks>
        public IReadOnlyList<Insertion> Selecting(Node target)
        {
            // The tree's own spans, never the whole statement — bracketing all of
            // it disambiguates nothing.
            var spans = target.Whole
                              .Where(node => node.Length > 0)
                              .Select(Range)
                              .Where(range => range.To - range.From < lexemes.Count)
                              .Distinct()
                              .ToList();

            // Bracket everything, and only go on if that pins the reading. It
            // should always — every subtree made explicit leaves one structure —
            // but a budget of zero cannot afford even this one resolution, and
            // then the reading is reported without a repair, honestly.
            if (Selects(target, spans) is false) return null;

            // Take a bracket away wherever the reading survives without it, widest
            // and rightmost first, so the wide outer groups go before the narrow
            // inner ones they were making redundant — which settles on the same
            // small, near-left bracketing the exhaustive search used to, without
            // its cost. Budget running out mid-trim keeps the extra brackets: a
            // fuller repair, never a wrong one.
            foreach (var span in spans.OrderByDescending(span => (span.To - span.From, span.From)).ToList())
            {
                if (Selects(target, spans.Where(kept => kept != span).ToList())) spans.Remove(span);
            }

            return Brackets(spans);
        }

        /// <summary>Whether bracketing these spans resolves uniquely to the target.</summary>
        private bool Selects(Node target, IReadOnlyList<(int From, int To)> spans)
        {
            var bracketed = Bracketed(spans);
            var key = string.Concat(bracketed.Select(lexeme => lexeme.Text + " "));

            if (resolved.TryGetValue(key, out var tree) is false)
            {
                if (spent >= budget) return false;

                ++spent;
                resolved[key] = tree = resolver.Resolve(bracketed).TryTree(out var only) ? only : null;
            }

            return tree is not null && Same(tree, target);
        }

        /// <summary>A node's lexeme range, found from its source extent.</summary>
        private (int From, int To) Range(Node node)
        {
            var from = 0;
            while (lexemes[from].Offset != node.Offset) ++from;

            var to = from;
            while (lexemes[to].Offset + lexemes[to].Length < node.Offset + node.Length) ++to;

            return (from, to + 1);
        }

        /// <summary>The lexemes with a bracket pair around each span.</summary>
        ///
        /// <remarks>
        ///     Walked by boundary rather than inserted per span, because the spans
        ///     NEST — a call's bracket contains its arguments' — and inserting each
        ///     pair independently shifted the indices of the ones around it. At
        ///     each gap the spans ending there close, innermost first, then the
        ///     spans starting there open, outermost first, so «(send (a to b))»
        ///     comes out nested rather than crossed.
        /// </remarks>
        private List<Lexeme> Bracketed(IReadOnlyList<(int From, int To)> spans)
        {
            List<Lexeme> bracketed = [];

            for (var at = 0; at <= lexemes.Count; ++at)
            {
                foreach (var _ in spans.Where(span => span.To == at).OrderByDescending(span => span.From))
                    bracketed.Add(new Lexeme(LexemeKind.Close, ")"));

                foreach (var _ in spans.Where(span => span.From == at).OrderByDescending(span => span.To))
                    bracketed.Add(new Lexeme(LexemeKind.Open, "("));

                if (at < lexemes.Count) bracketed.Add(lexemes[at]);
            }

            return bracketed;
        }

        /// <summary>The source edits that bracket each span, in the order to apply them.</summary>
        ///
        /// <remarks>
        ///     Walked by boundary, like <see cref="Bracketed"/>, so the pairs come
        ///     out ordered left to right and correctly nested — the spans nest, so
        ///     two brackets can land at one offset, and the order they are applied
        ///     in is then the difference between «(send (a to b))» and a crossed
        ///     pair. Applied left to right at their offsets, in this order.
        /// </remarks>
        private IReadOnlyList<Insertion> Brackets(IReadOnlyList<(int From, int To)> spans)
        {
            List<Insertion> inserts = [];

            for (var at = 0; at <= lexemes.Count; ++at)
            {
                if (at > 0)
                    foreach (var _ in spans.Where(span => span.To == at).OrderByDescending(span => span.From))
                        inserts.Add(new Insertion(lexemes[at - 1].Offset + lexemes[at - 1].Length, ")"));

                if (at < lexemes.Count)
                    foreach (var _ in spans.Where(span => span.From == at).OrderByDescending(span => span.To))
                        inserts.Add(new Insertion(lexemes[at].Offset, "("));
            }

            return Owned.Copy<Insertion>([.. inserts]);
        }
    }

    /// <summary>
    ///     Whether two trees are the same reading, ignoring the brackets a repair
    ///     added.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     A repair works by GROUPING, so what it produces is the target with a
    ///     bracket somewhere in it — never the target itself. Stripping the
    ///     brackets from both and asking whether they are then structurally the
    ///     same is what makes "did this select the reading" a question about the
    ///     reading, and it is <see cref="Node.Same"/>'s question once the
    ///     brackets are gone.
    ///     </para>
    ///     <para>
    ///     This compared RENDERINGS once, stripping bracket marks out of a string.
    ///     That recreated one layer later the very defect the cell was taught to
    ///     avoid: two calls spanning the same words render alike, so both searches
    ///     found the same bracket and one meaning was offered twice while the
    ///     other was unreachable.
    ///     </para>
    /// </remarks>
    private static bool Same(Node tree, Node target) => Node.Same.Equals(Stripped(tree), Stripped(target));

    /// <summary>
    ///     A tree with the brackets a repair added stripped away.
    /// </summary>
    ///
    /// <remarks>
    ///     A bracket around ONE value is a no-op grouping — «(x)» and «x» are the
    ///     same reading — so single non-collection groups come off, everywhere a
    ///     repair could put one: inside a call's arguments and an operator's
    ///     operands, which are the segmentation points an ambiguity turns on.
    ///     Nothing else is recursed into, because a repair does not bracket
    ///     inside it — a collection's element is its own reference, reported and
    ///     repaired on its own, and a name or literal has no inside.
    /// </remarks>
    private static Node Stripped(Node tree)
    {
        var bare = Bare(tree);

        if (bare is Node.Call call)
            return new Node.Call(call.Pattern, [.. call.Arguments.Select(Stripped)]);

        if (bare is Node.Operation operation)
            return new Node.Operation(Stripped(operation.Left), operation.Symbol, operation.Operator, Stripped(operation.Right));

        return bare;
    }

    /// <summary>A tree with the outermost repair brackets removed.</summary>
    private static Node Bare(Node tree)
        => tree is Node.Group { Collection: false, Parts.Count: 1 } group ? Bare(group.Parts[0]) : tree;
}
