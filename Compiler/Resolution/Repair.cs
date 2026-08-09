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

    /// <summary>The brackets to type, owned where the repair is made.</summary>
    ///
    /// <remarks>
    ///     Positional would have made the caller's list a public promise of its
    ///     own beside the owned one, which is the same value handed out twice
    ///     with only one of them safe.
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
///     one bracket, nor any fixed number of them, selects it. Sets of the tree's
///     spans are tried by increasing size, so a reading that pins N children is
///     reached at size N. Searching one pair, then exactly two, published an
///     empty repair for the first reading that needed three — a suggestion that
///     looks selectable and does nothing.
///     </para>
///     <para>
///     By RESOLVING each candidate rather than reasoning about the tree. The
///     claim a repair makes is "type this and the ambiguity is gone", so the
///     honest way to produce one is to type it and look. Each distinct candidate
///     is resolved once and cached, because two readings ask about the same
///     brackets.
///     </para>
///     <para>
///     BOUNDED. A set of size k costs «O(nᵏ)» candidates and each size is only
///     reached when every smaller one failed to select the reading; past a
///     budget of candidate resolutions the search stops and the reading is
///     reported without a repair — which is honest, where a hang is not, and the
///     readings are all there regardless. The budget ending the search is a
///     different thing from the search having no way to express a repair, which
///     is the state increasing the size removed.
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
        ///     The narrowest set of brackets that leaves only this reading.
        /// </summary>
        ///
        /// <remarks>
        ///     A reading is the whole statement's tree: selecting it means the
        ///     bracketed statement resolves uniquely and reads that way, ignoring
        ///     the brackets the repair itself added — «Same» unwraps those.
        /// </remarks>
        public IReadOnlyList<Insertion> Selecting(Node target)
        {
            // The tree's own spans, narrowest first for minimality, and never the
            // whole statement — bracketing all of it disambiguates nothing.
            var spans = target.Whole
                              .Where(node => node.Length > 0)
                              .Select(node => Range(node))
                              .Where(range => range.To - range.From < lexemes.Count)
                              .Distinct()
                              .OrderBy(range => range.To - range.From)
                              .ToArray();

            // Non-overlapping sets of those spans, by increasing size and then by
            // total width — the narrowest single first, then the cheapest pair,
            // then triples and beyond. A reading of an expression with N
            // independently ambiguous children fixes a meaning for all of them at
            // once and needs a bracket around each, so stopping at one or two
            // left every such reading with no selectable repair — a single pair
            // and a fixed pair were the same fixed-arity assumption a step apart.
            //
            // Each further size multiplies the candidates, and the budget — shared
            // across this statement's readings and counted in resolutions — is
            // what ends the search: a reading whose brackets cost more than is
            // left is reported without one, which is honest where a hang is not,
            // and distinguishes "the budget ran out" from the old "the search had
            // no way to express this".
            for (var size = 1; size <= spans.Length && spent < budget; ++size)
            {
                foreach (var set in Disjoint(spans, size).OrderBy(Total))
                {
                    if (Selects(target, set)) return Brackets(set);
                }
            }

            return null;
        }

        /// <summary>The total width of a set of spans, its cost as a repair.</summary>
        private static int Total((int From, int To)[] set) => set.Sum(span => span.To - span.From);

        /// <summary>
        ///     Every set of a given size of pairwise non-overlapping spans.
        /// </summary>
        ///
        /// <remarks>
        ///     Non-overlapping because <see cref="Bracketed"/> inserts a pair
        ///     around each span independently, which two spans sharing a word
        ///     would interleave into a mispairing. The tree's spans nest — a call
        ///     contains its arguments — so overlapping combinations are the
        ///     common case and pruned here rather than resolved and rejected.
        /// </remarks>
        private static IEnumerable<(int From, int To)[]> Disjoint((int From, int To)[] spans, int size)
            => Extending(spans, size, 0, []);

        private static IEnumerable<(int From, int To)[]> Extending((int From, int To)[] spans, int size,
                                                                   int from, List<(int From, int To)> chosen)
        {
            if (chosen.Count == size)
            {
                yield return [.. chosen];
                yield break;
            }

            for (var at = from; at < spans.Length; ++at)
            {
                if (chosen.Any(span => Overlaps(span, spans[at]))) continue;

                chosen.Add(spans[at]);

                foreach (var set in Extending(spans, size, at + 1, chosen)) yield return set;

                chosen.RemoveAt(chosen.Count - 1);
            }
        }

        /// <summary>Whether two spans share a lexeme.</summary>
        private static bool Overlaps((int From, int To) a, (int From, int To) b)
            => a.From < b.To && b.From < a.To;

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
        private List<Lexeme> Bracketed(IReadOnlyList<(int From, int To)> spans)
        {
            List<Lexeme> bracketed = [.. lexemes];

            // Right to left, so an earlier span's indices are untouched by a
            // later one's brackets.
            foreach (var span in spans.OrderByDescending(span => span.From))
            {
                bracketed.Insert(span.To, new Lexeme(LexemeKind.Close, ")"));
                bracketed.Insert(span.From, new Lexeme(LexemeKind.Open, "("));
            }

            return bracketed;
        }

        /// <summary>The source edits that bracket each span.</summary>
        private IReadOnlyList<Insertion> Brackets(IReadOnlyList<(int From, int To)> spans)
            => Owned.Copy<Insertion>(
               [.. spans.SelectMany(span => new[]
               {
                   new Insertion(lexemes[span.From].Offset, "("),
                   new Insertion(lexemes[span.To - 1].Offset + lexemes[span.To - 1].Length, ")"),
               })]);
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
