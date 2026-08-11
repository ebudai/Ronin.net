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
///     one bracket, nor any fixed number of them, selects it. So the brackets are
///     grown one at a time, from where the readings disagree, until only the
///     target is left. Searching one pair, then exactly two, published an empty
///     repair for the first reading that needed three.
///     </para>
///     <para>
///     By RESOLVING each candidate rather than reasoning about the tree. The
///     claim a repair makes is "type this and the ambiguity is gone", so the
///     honest way to produce one is to type it and look. The statement is
///     re-resolved after each bracket, which is what finds the reading to rule
///     out next — including one the display cap hid until the cheaper choices
///     were pinned. Each distinct candidate is resolved once and cached.
///     </para>
///     <para>
///     PROPORTIONAL to the answer. Only a subtree two readings disagree on is
///     bracketed, and the deepest such, and never one they share — so the large
///     unambiguous argument is never entered, every bracket is one the answer
///     needs, and the candidate is the answer as it grows. Bracketing every
///     subtree and trimming, before, made a candidate past the resolver's lexeme
///     ceiling; and a width order deferred the one wide bracket that did the work
///     behind every narrow idle one. The budget is lexemes resolved, an editor's
///     budget rather than a count blind to how long each resolution is; past it a
///     reading is reported without a repair, honest where a hang is not.
///     </para>
/// </remarks>
internal static class Repairs
{
    /// <summary>The lexemes a statement's repairs may resolve, across every candidate.</summary>
    ///
    /// <remarks>
    ///     An editor's budget: bounded WORK, not a bounded tally of calls. A count
    ///     of resolutions did not see how long each one was, so a long expression
    ///     spent seconds inside a small number of them. Forty thousand lexemes
    ///     fully repairs every reading of an expression up to twenty independently
    ///     ambiguous children — well past anything written on purpose — and past
    ///     it a reading is reported without a repair rather than the editor
    ///     waiting on work that grows with the square of the statement.
    /// </remarks>
    private const int Budget = 40_000;

    public static IReadOnlyList<Repair> For(Resolver resolver, IReadOnlyList<Lexeme> lexemes, Resolution ambiguity)
        => For(resolver, lexemes, ambiguity, Budget);

    /// <summary>
    ///     The repairs, resolving at most <paramref name="budget"/> lexemes across
    ///     every candidate.
    /// </summary>
    ///
    /// <remarks>
    ///     The budget is a parameter only so a test can set it low enough to
    ///     reach the guard deterministically — the pathological statement that
    ///     spends the whole budget is near the lexeme limit and slow to
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
        private readonly Dictionary<string, Resolution> resolved = [];
        private int spent;

        /// <summary>
        ///     A small set of brackets that leaves only this reading.
        /// </summary>
        ///
        /// <remarks>
        ///     <para>
        ///     GROWN from where the readings disagree, and re-resolved after each
        ///     bracket. The bracketed statement is resolved; wherever the target
        ///     and a surviving reading segment a subtree differently, a bracket is
        ///     added around the target's, ruling that reading out; and it is
        ///     resolved again. No trim after: every bracket added is one a reading
        ///     disagreed on, so none is idle to take back.
        ///     </para>
        ///     <para>
        ///     RE-RESOLVED, because the readings it must rule out are more than it
        ///     can see. «Alternatives» is capped for display, and ordering a
        ///     bracket by whether one of those few lacks it called a bracket every
        ///     shown reading happens to share idle — even when a dearer reading the
        ///     cap hid still needs ruling out. Growing through the unambiguous
        ///     spans that share left a candidate past the resolver's ceiling, and
        ///     no repair. Re-resolving brings the hidden reading into view: pinning
        ///     the cheaper choices first makes the dearer one the surviving
        ///     ambiguity, and the bracket that rules it out is found where it is.
        ///     </para>
        ///     <para>
        ///     PROPORTIONAL to the answer. Only a subtree the readings disagree on
        ///     is bracketed — never the large unambiguous argument, nor an argument
        ///     the competitor shares, which was a surplus pair enough to turn a
        ///     valid answer too long at the ceiling — so the candidate is the
        ///     answer as it grows, a resolution per bracket and none wasted.
        ///     </para>
        /// </remarks>
        public IReadOnlyList<Insertion> Selecting(Node target)
        {
            List<(int From, int To)> spans = [];

            while (true)
            {
                // Over budget: the reading is reported without a repair, honestly.
                if (Resolve(spans) is not Resolution resolution) return null;

                // Only the target left — done growing.
                if (resolution.TryTree(out var tree) && Same(tree, target)) break;

                // A subtree the target and a surviving reading disagree on. None
                // means the search cannot express this repair — it returns nothing
                // rather than loop.
                if (Diverging(target, resolution, spans) is not (int From, int To) span) return null;

                spans.Add(span);
            }

            // No trim: only a subtree the readings disagree on is ever added, and
            // the deepest such, so every bracket is one the answer needs — where
            // bracketing every subtree and trimming, or growing by a width order,
            // added idle ones a trim then had to take back.
            return Brackets(spans);
        }

        /// <summary>The bracketed statement resolved, or nothing when it costs too much.</summary>
        ///
        /// <remarks>
        ///     The NEXT charge is tested before it is made, by subtraction so a
        ///     near-limit budget cannot overflow — so «at most budget lexemes»
        ///     holds, where checking only what was already spent admitted one whole
        ///     candidate past it. Charged the lexemes it resolves, not one flat
        ///     count, because that is the work: a resolution's cost grows with the
        ///     statement, and a budget counting resolutions let a long expression
        ///     spend seconds inside a small number of them. A budget in lexemes is
        ///     an editor's, on bounded work rather than a tally of calls whose size
        ///     it does not see.
        /// </remarks>
        private Resolution? Resolve(IReadOnlyList<(int From, int To)> spans)
        {
            var bracketed = Bracketed(spans);
            var key = string.Concat(bracketed.Select(lexeme => lexeme.Text + " "));

            if (resolved.TryGetValue(key, out var resolution) is false)
            {
                if (bracketed.Count > budget - spent) return null;

                spent += bracketed.Count;
                resolved[key] = resolution = resolver.Resolve(bracketed);
            }

            return resolution;
        }

        /// <summary>
        ///     A target subtree to bracket that a surviving reading disagrees on.
        /// </summary>
        ///
        /// <remarks>
        ///     The competing readings the resolver kept for the current bracketing,
        ///     walked against the target until their structures part. Bracketing
        ///     the target's subtree there forces its grouping and rules that
        ///     reading out. Not the spans already added, since bracketing one that
        ///     is already bracketed would not move the search. The resolution is
        ///     always the ambiguous one — bracketing only the target's own subtrees
        ///     keeps the target a reading, so a unique resolution is always the
        ///     target, and then the loop has already stopped.
        /// </remarks>
        private (int From, int To)? Diverging(Node target, Resolution resolution, IReadOnlyList<(int From, int To)> avoid)
        {
            foreach (var competitor in resolution.Alternatives)
            {
                if (Same(competitor, target)) continue;

                if (Divergence(target, competitor, avoid) is (int From, int To) span) return span;
            }

            return null;
        }

        /// <summary>
        ///     Where a target subtree and a competitor's segment it differently,
        ///     as a target span to bracket.
        /// </summary>
        ///
        /// <remarks>
        ///     <para>
        ///     The same subtrees «Stripped» keeps: a collection or a lookup is
        ///     opaque, bracketed around and never inside. Where the target and the
        ///     competitor are the same reading, there is nothing to bracket.
        ///     </para>
        ///     <para>
        ///     At a call, the target's arguments are matched to the competitor's by
        ///     the WORDS THEY OCCUPY, not by position or value. An argument the
        ///     competitor covers with one of its own is the same boundary, walked
        ///     into for a disagreement deeper down; an argument whose words the
        ///     competitor segments differently is the boundary the two disagree on,
        ///     and bracketing it — the argument, not the call, whose own words would
        ///     stay free to regroup — rules that reading out. Comparing by value
        ///     instead made a repeated name look shared: «f a with a end» has «a»
        ///     twice, and the second, whose words the competitor reads as «a end»,
        ///     matched the competitor's first «a» and went unbracketed, so a valid
        ///     repair was dropped. And matching a shared argument earlier by
        ///     PATTERN missed the boundary «f a with b end» disagrees on, a surplus
        ///     pair enough at the ceiling to turn a valid answer too long — which is
        ///     why there is no trim: only a bracket a reading disagrees on is added.
        ///     </para>
        /// </remarks>
        private (int From, int To)? Divergence(Node target, Node competitor, IReadOnlyList<(int From, int To)> avoid)
        {
            var t = Bare(target);
            var c = Bare(competitor);

            if (Node.Same.Equals(Stripped(t), Stripped(c))) return null;

            if (t is Node.Operation left && c is Node.Operation right && left.Symbol == right.Symbol)
                return Divergence(left.Left, right.Left, avoid) ?? Divergence(left.Right, right.Right, avoid);

            var others = c is Node.Call call ? call.Arguments : [];

            foreach (var argument in t is Node.Call diverging ? diverging.Arguments : [t])
            {
                var span = Where(argument);

                if (span.To - span.From >= lexemes.Count) continue;   // the whole statement disambiguates nothing

                if (others.FirstOrDefault(other => Where(other) == span) is Node aligned)
                {
                    // Same boundary — a disagreement, if any, is deeper, and must
                    // be searched for even inside a pair already added: «avoid»
                    // stops the same pair being returned twice, not the walk from
                    // going beneath it.
                    if (Divergence(argument, aligned, avoid) is (int From, int To) deeper) return deeper;
                }
                else if (avoid.Contains(span) is false)
                {
                    // The competitor segments these words differently — bracket
                    // them, unless that pair is already there.
                    return span;
                }
            }

            return null;
        }

        /// <summary>A subtree's lexeme range, the brackets a repair added stripped off it.</summary>
        private (int From, int To) Where(Node node) => Range(Bare(node));

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
        ///     <para>
        ///     Walked by boundary rather than inserted per span, because the spans
        ///     NEST — a call's bracket contains its arguments' — and inserting each
        ///     pair independently shifted the indices of the ones around it. At
        ///     each gap the spans ending there close, innermost first, then the
        ///     spans starting there open, outermost first, so «(send (a to b))»
        ///     comes out nested rather than crossed.
        ///     </para>
        ///     <para>
        ///     Each bracket carries the source position of the gap it sits in — a
        ///     close at the end of the lexeme before it, an open at the start of
        ///     the one after — rather than the default of zero. The resolver reads
        ///     a node's extent off its first and last lexeme, so a call whose last
        ///     lexeme is a synthetic close at position zero got a length reaching
        ///     back to the start of the file, and «Where» then read that call's
        ///     range as something no argument aligned with — a surplus bracket, and
        ///     a repair one level deeper lost. A zero-WIDTH marker at the true
        ///     boundary leaves every enclosing extent the source's.
        ///     </para>
        /// </remarks>
        private List<Lexeme> Bracketed(IReadOnlyList<(int From, int To)> spans)
        {
            List<Lexeme> bracketed = [];

            for (var at = 0; at <= lexemes.Count; ++at)
            {
                foreach (var _ in spans.Where(span => span.To == at).OrderByDescending(span => span.From))
                    bracketed.Add(new Lexeme(LexemeKind.Close, ")", Offset: lexemes[at - 1].Offset + lexemes[at - 1].Length));

                foreach (var _ in spans.Where(span => span.From == at).OrderByDescending(span => span.To))
                    bracketed.Add(new Lexeme(LexemeKind.Open, "(", Offset: lexemes[at].Offset));

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
        => tree is Node.Group { Kind: Node.Grouping.Group, Parts.Count: 1 } group ? Bare(group.Parts[0].Value) : tree;
}
