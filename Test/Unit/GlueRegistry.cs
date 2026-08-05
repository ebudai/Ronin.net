// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Runtime;

namespace Unit;

/// <summary>
///     The reserved-word list, as a file that fails the build when it changes.
/// </summary>
///
/// <remarks>
///     <para>
///     Adding a pattern with a word after an INDETERMINATE hole reserves that
///     word in every scope the pattern reaches — names that were legal stop
///     being legal. Nothing noticed that before, which is the whole reason this
///     exists: it turns "we silently broke everyone's names" into a reviewable
///     diff. A hole that cannot grow — pinned, or required to be bracketed —
///     costs nothing, which is why the current registry reserves no word at all.
///     </para>
///     <para>
///     Generated from the language's own patterns rather than transcribed, so it
///     cannot drift from what the compiler actually reserves. The seed registry
///     in the handoff folder is the designer's, computed from an aspirational
///     stdlib; this one is computed from what exists.
///     </para>
/// </remarks>
[Trait(nameof(Glue), null)]
public class GlueRegistry
{
    private static readonly string Committed =
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "docs", "reserved-words.txt");

    [Fact(DisplayName = "every injected shape joins on a protected word")]
    public void EveryInjectedShapeJoinsOnAProtectedWord()
    {
        // The two halves of one rule: glue words may not be names, and injection
        // words may not be glue. A shape that joined on an ordinary word could
        // be broken by a declaration somewhere else — the first «wait until»
        // spelling used «in flight», and «in» is the word most likely to become
        // glue again, so that trap would have fired on every «wait until» in
        // every program at once.
        var kept = new HashSet<string>(Rules.Injected.Select(injection => injection.Word))
        {
            SymbolTable.Old,
        };

        // Over the REAL descriptors, which the builders, the rule and the
        // registry all read. Iterating the registry checked the sample against
        // the rule and said nothing about the implementation: an injector left
        // out of the registry kept it green, which is the same defect as a
        // hand-built token chain standing in for source.
        Assert.NotEmpty(Injection.All);

        foreach (var injection in Injection.All)
        {
            // every word of it, not merely those before the first placeholder
            Assert.All(injection.Words, word => Assert.Contains(word, kept));

            // and the name it builds is those words and the subject, so nothing
            // can be in the shape that is not accounted for above
            Assert.Equal(injection.Of("x"), injection.Prefix + "x");
        }

        // And a chain's names are not among them, because nothing it generates
        // is typed: they are reports, and a report cannot collide with a
        // declaration. Making them prose cost three protected words to dress
        // internal identifiers as a language they were never part of.
        Assert.DoesNotContain(Graph.Waiting("when a", 1), Glue.Shapes.Select(shape => shape.Shape));
    }

    [Fact(DisplayName = "the registry matches what the language reserves")]
    public void TheRegistryMatchesWhatTheLanguageReserves()
    {
        var registry = Glue.Registry(SymbolTable.Builtins);

        Assert.True(File.Exists(Committed), $"{Committed} is missing — regenerate it");

        // Normalised, because a file's line endings are the checkout's business
        // and not the language's.
        Assert.Equal(File.ReadAllText(Committed).ReplaceLineEndings("\n"),
                     registry.ReplaceLineEndings("\n"));
    }

    [Fact(DisplayName = "a word before the first hole reserves nothing")]
    public void AWordBeforeTheFirstHoleReservesNothing()
    {
        // The design lever, and the reason the registry lists the free patterns
        // beside the costly ones: the shape to prefer is only visible next to
        // the shape that charges for itself.
        Assert.Empty(Glue.Reserved([Pattern.Parse("compute total for _"), Pattern.Parse("sum of _")]));

        Assert.Equal([("to", "send (_) to (_)")],
                     Glue.Reserved([Pattern.Parse("send _ to _")]));

        // Consecutive glue words are each reserved: only the one directly after
        // a PINNED hole is protected by it, and the protection does not carry
        // along the run.
        Assert.Equal([("over", "repeat (_) times over"), ("times", "repeat (_) times over")],
                     Glue.Reserved([Pattern.Parse("repeat _ times over")]));
    }

    [Fact(DisplayName = "a pinned hole protects the word after it, and only that one")]
    public void APinnedHoleProtectsTheWordAfterItAndOnlyThatOne()
    {
        // The whole of what pinning buys. The pinned loop reserves nothing,
        // where the free-hole version reserved «in» — and a second word further
        // along would still be reserved, because nothing is pinned in front of
        // it.
        Assert.Empty(Glue.Reserved([new Pattern(["for each", null, "in", null], [1])]));
        Assert.Equal(["in"], new Pattern(["for each", null, "in", null]).Glue);

        Assert.Equal([("over", "take «one word, or a bracketed name» in (_) over")],
                     Glue.Reserved([new Pattern(["take", null, "in", null, "over"], [1])]));
    }

    [Fact(DisplayName = "the registry shows what is free beside what is not")]
    public void TheRegistryShowsWhatIsFreeBesideWhatIsNot()
    {
        // Both halves, because the shape to prefer is only legible next to the
        // shape that charges. A registry listing only the costs would read as a
        // tax nobody can avoid, when in fact avoiding it is a respelling.
        var registry = Glue.Registry([Pattern.Parse("send _ to _"), Pattern.Parse("sum of _")]);

        Assert.Contains("## RESERVED (1)", registry);
        Assert.Contains("to           send (_) to (_)", registry);

        Assert.Contains("## RESERVES NOTHING (1)", registry);
        Assert.Contains("    sum of (_)", registry);
    }

    [Fact(DisplayName = "one word reserved by two patterns is named by both")]
    public void OneWordReservedByTwoPatternsIsNamedByBoth()
    {
        // Which pattern cost you the word is the actionable half of the message,
        // so a word reserved twice has to say so twice — respelling one of them
        // does not give it back.
        Assert.Equal(
            [("of", "item (_) of (_)"), ("of", "part (_) of (_)")],
            Glue.Reserved([Pattern.Parse("part _ of _"), Pattern.Parse("item _ of _")]));
    }

    [Fact(DisplayName = "the most expensive shape cannot be built at all")]
    public void TheMostExpensiveShapeCannotBeBuiltAtAll()
    {
        // A leading hole means an empty anchor run, so EVERY word in the pattern
        // is glue — postfix is the costliest shape there is. The language refuses
        // it outright rather than pricing it, which is why the registry can never
        // contain one. Recorded here because "it would have been expensive" is
        // the reason the refusal exists.
        Assert.Throws<ArgumentException>(() => new Pattern([null, "rounded"]));

        // and the same shape from source is a finding rather than a throw
        var finding = Assert.Single(Compilation.Of(
            new SourceText("function (x => Number) rounded { return x; }\n", "Player.ron")).Findings);

        Assert.Equal("(_) rounded", Assert.IsType<LeadingHole>(finding).Pattern);
    }

    [Fact(DisplayName = "and a pattern refined by another reserves that word as a name prefix")]
    public void AndAPatternRefinedByAnotherReservesThatWordAsANamePrefix()
    {
        // Found by audit. R7b was a relationship computed privately inside one
        // rule, so the generated file that says what the language reserves could
        // not see it — and told a reader that «all» is ordinary glue, free at an
        // edge, while validation refused every name beginning with it. The file
        // said the opposite of the rule.
        //
        // A SYNTHETIC pair, because the builtin table has one pattern and one
        // pattern makes no pair. The checked-in file stays the change detector
        // for what the language actually ships.
        var registry = Glue.Registry([Pattern.Parse("send _ to _"), Pattern.Parse("send _ to all _")]);

        Assert.Contains("## RESERVES A NAME PREFIX BY REFINING (1)", registry);
        Assert.Contains("all          send (_) to all (_) is send (_) to (_) with it at a hole", registry);
    }

    [Fact(DisplayName = "and a pattern the compiler refuses reserves nothing here either")]
    public void AndAPatternTheCompilerRefusesReservesNothingHereEither()
    {
        // Found by audit. This file's header says these are the patterns in
        // scope and that adding a line is a breaking change — so a reservation
        // from a pattern that cannot enter the language is worse than no report
        // at all. «send (_) to otherwise (_)» uses an operator word and is
        // refused; it reserved «otherwise» here anyway, because soundness was a
        // predicate private to the rules while this built its tables from
        // everything it was handed.
        //
        // BOTH SIDES asserted, because the point is that they agree: one
        // structural finding from validation, and nothing claimed here.
        Pattern[] patterns = [Pattern.Parse("send _ to _"), Pattern.Parse("send _ to otherwise _")];

        var findings = Compilation.Of(new SourceText(
            "var otherwise things => Number;\n"
          + "function send (x => Number) to (y => Number) { return x; }\n"
          + "function send (x => Number) to otherwise (y => Number) { return x; }\n",
            "Player.ron")).Findings;

        Assert.Equal(nameof(InfixInPattern), Assert.Single(findings).GetType().Name);

        Assert.Contains("## RESERVES A NAME PREFIX BY REFINING (0)", Glue.Registry(patterns));
        Assert.DoesNotContain("otherwise    send", Glue.Registry(patterns));
    }
}
