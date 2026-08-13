// Copyright © 2026 Eric Budai

using Ronin.Compiler;

namespace Unit;

/// <summary>
///     The resolver counts a statement's meanings the way an independent
///     enumerator does.
/// </summary>
///
/// <remarks>
///     <para>
///     The count is the number the whole ambiguity story rests on, and the older
///     completeness test took it from the resolver and then checked only the
///     readings that same resolver returned — so a statement the resolver
///     miscounted was never noticed, and one whole class of bug did exactly that:
///     two calls spanning the same words print the same sentence, were deduped by
///     that sentence, and a statement with two meanings arrived reading as one.
///     </para>
///     <para>
///     This counts the meanings a SECOND way — a plain recurrence over the same
///     grammar, with no chart and no cost — and holds the resolver's «Total» to
///     it. A meaning is a parse TREE, so the enumerator counts distinct trees, not
///     distinct renderings: «print send a to b» has three trees and two of them
///     render alike, and the count is three. That is the exact shape the deduped
///     bug got wrong, and nothing about how a tree reads enters the count here.
///     </para>
///     <para>
///     EXACT where the resolver's count is exact, and a floor where it is a floor:
///     past the keep cap «Total» is a stated lower bound, and there the
///     enumerator's exact count must be at least it — a floor that claimed more
///     meanings than exist would be as wrong as a miscount. The number of each is
///     asserted, because a count property that meets no ambiguity, and a floor
///     property that meets no floor, both pass forever.
///     </para>
/// </remarks>
[Trait(nameof(Resolver), null)]
public class MeaningCount
{
    private const string Declarations =
        "function wrap (x => number) { return x; }\n"
      + "function print (x => number) { return x; }\n"
      + "function print (x => number) to (y => number) { return x; }\n"
      + "function send (x => number) { return x; }\n"
      + "function send (x => number) to (y => number) { return x; }\n"
      + "var a => number;\nvar b => number;\nvar a to b => number;\nvar b to a => number;\n";

    private const string Result = "var result = ";

    /// <summary>The declared names, as the word sequences the enumerator matches.</summary>
    private static readonly string[][] names =
        [["a"], ["b"], ["a", "to", "b"], ["b", "to", "a"]];

    /// <summary>The declared patterns, as segment lists where a null is a free hole.</summary>
    private static readonly string[][] patterns =
    [
        ["wrap", null],
        ["print", null],
        ["print", null, "to", null],
        ["send", null],
        ["send", null, "to", null],
    ];

    private static readonly string[] callers = ["wrap", "print", "send"];
    private static readonly string[] leaves = ["a", "b", "a to b", "b to a"];
    private static readonly string[] ends = ["a", "b"];

    private static string Expression(Random random)
    {
        List<string> words = [];

        for (var caller = random.Next(0, 4); caller > 0; --caller) words.Add(callers[random.Next(callers.Length)]);

        words.Add(leaves[random.Next(leaves.Length)]);

        for (var tail = random.Next(0, 3); tail > 0; --tail) { words.Add("to"); words.Add(ends[random.Next(ends.Length)]); }

        return string.Join(' ', words);
    }

    /// <summary>
    ///     Distinct parse trees of <c>words[from..to)</c>: a span is a name, or a
    ///     call whose anchors match literally and whose free holes each take a
    ///     sub-span that is itself an expression.
    /// </summary>
    ///
    /// <remarks>
    ///     Two different top choices — a different pattern, a different hole
    ///     split, a different sub-tree — are different trees, so the counts add
    ///     with no double counting, and a name is never a call. Memoised on the
    ///     span, which is what keeps it a recurrence rather than an exponential
    ///     walk.
    /// </remarks>
    private static long Trees(string[] words, int from, int to, Dictionary<(int, int), long> memo)
    {
        if (memo.TryGetValue((from, to), out var cached)) return cached;

        long total = 0;

        foreach (var name in names)
            if (to - from == name.Length && Segment(words, from, to).SequenceEqual(name)) ++total;

        foreach (var pattern in patterns) total += Match(words, pattern, 0, from, to, memo);

        return memo[(from, to)] = total;
    }

    private static IEnumerable<string> Segment(string[] words, int from, int to)
    {
        for (var at = from; at < to; ++at) yield return words[at];
    }

    /// <summary>Ways to match <c>pattern[segment..]</c> over <c>words[pos..end)</c>.</summary>
    private static long Match(string[] words, string[] pattern, int segment, int pos, int end, Dictionary<(int, int), long> memo)
    {
        if (segment == pattern.Length) return pos == end ? 1 : 0;

        if (pattern[segment] is string literal)
            return pos < end && words[pos] == literal ? Match(words, pattern, segment + 1, pos + 1, end, memo) : 0;

        long ways = 0;

        // A free hole takes one or more words, and the rest of the pattern must
        // match what is left.
        for (var cut = pos + 1; cut <= end; ++cut)
        {
            var inside = Trees(words, pos, cut, memo);

            if (inside is not 0) ways += inside * Match(words, pattern, segment + 1, cut, end, memo);
        }

        return ways;
    }

    private static long Counted(string statement)
    {
        var words = statement.Split(' ');

        return Trees(words, 0, words.Length, []);
    }

    private static Resolution Resolved(string statement)
    {
        var source = Declarations + Result + statement + ";\n";
        var at = Declarations.Length + Result.Length;

        return Compilation.Of(new SourceText(source, "gen.ron")).Readings
                          .Single(reading => reading.Span.Offset == at)
                          .Resolution;
    }

    [Fact(DisplayName = "the resolver's meaning count matches an independent enumeration")]
    public void TheResolversMeaningCountMatchesAnIndependentEnumeration()
    {
        Random random = new(Seed: 20260810);

        List<string> diverging = [];
        var ambiguous = 0;
        var bounded = 0;

        for (var trial = 0; trial < 1000; ++trial)
        {
            var statement = Expression(random);
            var resolution = Resolved(statement);
            var oracle = Counted(statement);

            if (resolution.Kind is ResolutionKind.TooLong) continue;

            // Past the keep cap the count is a floor, so the exact enumeration
            // must be at least it, not equal to it.
            if (resolution.Bounded)
            {
                ++bounded;
                if (oracle < resolution.Total) diverging.Add($"«{statement}»: enumerated {oracle} below the floor {resolution.Total}");
                continue;
            }

            var counted = resolution.Kind switch
            {
                ResolutionKind.Resolved => 1L,
                ResolutionKind.Ambiguous => resolution.Total,
                _ => 0L,
            };

            if (resolution.Kind is ResolutionKind.Ambiguous) ++ambiguous;

            if (oracle != counted) diverging.Add($"«{statement}»: enumerated {oracle}, resolver {counted} ({resolution.Kind})");
        }

        Assert.Empty(diverging);
        Assert.Equal(329, ambiguous);
        Assert.Equal(29, bounded);
    }
}
