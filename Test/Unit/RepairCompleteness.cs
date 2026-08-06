// Copyright © 2026 Eric Budai

using Ronin.Compiler;

namespace Unit;

/// <summary>
///     Every reading of an ambiguous statement can be selected by bracketing.
/// </summary>
///
/// <remarks>
///     <para>
///     The property the whole direction rests on. Ambiguity is an error that
///     offers the bracketings, so a reading no bracket can select is a program
///     nobody can write — and nothing else in the suite would notice, because
///     every statement still resolves or reports and no assertion anywhere asks
///     whether the repair exists.
///     </para>
///     <para>
///     It is what kept two of the name rules. «a to b» reads only as itself, so
///     the ambiguity it causes is in some other statement and a bracket there
///     reaches it; «a is b» and «send a» read as something else over their OWN
///     span, so no bracket selects them and they stay refused at the
///     declaration. This test is the evidence for that split rather than a
///     restatement of it: admit either one and it fails.
///     </para>
///     <para>
///     GENERATED and exhaustive, not fixtured. Both errors this direction went
///     through were found by generation and neither would have been found by
///     hand — one of them was a per-rule check reporting PASS over zero cases,
///     which is why each row asserts its exact count of ambiguous statements
///     rather than printing it.
///     </para>
/// </remarks>
[Trait(nameof(Resolver), null)]
public class RepairCompleteness
{
    private static SymbolTable Table(string names, string patterns)
    {
        SymbolTable symbols = new();

        symbols.WithNames([.. Split(names)]).WithPatterns([.. Split(patterns)]);

        return symbols;
    }

    private static IEnumerable<string> Split(string list)
        => list.Split(',').Select(entry => entry.Trim());

    private static IEnumerable<string[]> Statements(string[] words, int length)
    {
        if (length is 0)
        {
            yield return [];
            yield break;
        }

        foreach (var rest in Statements(words, length - 1))
        {
            foreach (var word in words) yield return [.. rest, word];
        }
    }

    /// <summary>Whether one bracket pair anywhere makes <paramref name="reading"/> the only reading.</summary>
    ///
    /// <remarks>
    ///     A reading is a fragment: the witness carries the competing readings of
    ///     the span where the tie IS, which a parent cannot see. So selecting one
    ///     means the whole statement resolves uniquely and reads that way over
    ///     that span, not that the whole rendering equals it.
    /// </remarks>
    private static bool Selectable(SymbolTable symbols, string[] statement, string reading)
    {
        for (var from = 0; from < statement.Length; ++from)
        {
            for (var to = from + 1; to <= statement.Length; ++to)
            {
                var bracketed = string.Join(' ', statement[..from]
                                                     .Concat(["("])
                                                     .Concat(statement[from..to])
                                                     .Concat([")"])
                                                     .Concat(statement[to..]));

                var resolved = new Resolver(symbols).Resolve(Lexemes.Lex(bracketed));

                if (resolved.Kind is not ResolutionKind.Resolved) continue;
                if (Bare(resolved.Reading).Contains(reading)) return true;
            }
        }

        return false;
    }

    private static string Bare(string reading)
        => reading.Replace("⟨", string.Empty).Replace("⟩", string.Empty);

    /// <param name="vocabulary">Five words, every sequence of two to six: 19,525 statements.</param>
    ///
    /// <param name="names">
    ///     Everything the deleted rules used to refuse — interior glue «a to b»,
    ///     the all-glue «to to», the edge forms — so the property is tested on
    ///     exactly what the deletion admits. Nothing the surviving rule refuses.
    /// </param>
    ///
    /// <param name="ambiguities">
    ///     Exact, because a property test that proves the property over zero
    ///     cases passes forever. The check that sent this direction down the
    ///     wrong path reported "all readings expressible" on no cases at all.
    ///     <para>
    ///     These were 20 and 24, and they were counting the resolver's mistakes
    ///     as agreement: two call trees that rendered alike were one derivation,
    ///     so a statement with two meanings arrived here Resolved and was never
    ///     examined. More than half the ambiguity in this space was invisible.
    ///     The property held over the larger set unchanged, which is worth more
    ///     than it holding over the smaller one did.
    ///     </para>
    /// </param>
    ///
    /// <remarks>
    ///     Two vocabularies because neither alone covers both halves of the
    ///     surviving rule. The first generates no «is», so it never exercises an
    ///     operator word; the second has no second anchor, so it never exercises
    ///     one pattern's words leading another's call.
    /// </remarks>
    [Theory(DisplayName = "every reading of an ambiguous statement is selectable by bracketing")]
    [InlineData("a, b, to, send, print", "a, b, a to b, b to a, to a, to to", "print _, send _ to _, send _, sum of _", 50)]
    [InlineData("a, b, to, send, is", "a, b, to, a to b, to to, to b, a to", "send _, send _ to _", 55)]
    public void EveryReadingOfAnAmbiguousStatementIsSelectableByBracketing(string vocabulary, string names, string patterns, int ambiguities)
    {
        string[] words = [.. Split(vocabulary)];

        var symbols = Table(names, patterns);
        var resolver = new Resolver(symbols);

        List<string> unreachable = [];
        var ambiguous = 0;
        var total = 0;

        for (var length = 2; length <= 6; ++length)
        {
            foreach (var statement in Statements(words, length))
            {
                ++total;

                var source = string.Join(' ', statement);
                var resolution = resolver.Resolve(Lexemes.Lex(source));

                if (resolution.Kind is not ResolutionKind.Ambiguous) continue;

                ++ambiguous;

                foreach (var reading in resolution.Readings)
                {
                    if (Selectable(symbols, statement, reading) is false) unreachable.Add($"«{source}» cannot express {reading}");
                }
            }
        }

        Assert.Equal(19525, total);
        Assert.Equal(ambiguities, ambiguous);
        Assert.Empty(unreachable);
    }
}
