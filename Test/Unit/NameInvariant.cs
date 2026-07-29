// Copyright © 2026 Eric Budai

using Ronin.Compiler;

namespace Unit;

/// <summary>
///     A name is a word-only span. Everything about reserved words rests on it.
/// </summary>
///
/// <remarks>
///     <para>
///     The exhaustive searches counted TIES, and a tie is all any of them
///     measured. Every figure carries a scope, because quoting one without it is
///     how this got misread once already:
///     </para>
///     <code>
///     2,382,240    anchor-first word patterns, no brackets      the original run
///     45,131,520   single patterns and pairs, ≤ 2 units         the bracket runs
///     294,333,696  pattern pairs, ≤ 3 units                     both policies
///     4,083,840    the original's configuration, new machinery  the baseline
///     </code>
///     <para>
///     Every one of them is reproducible from the handoff folder — the original
///     harness was the last to arrive and confirms its own figure at 91 pattern
///     pairs, 24 of them refused by R6.
///     </para>
///     <para>
///     R5's actual purpose is preventing silent CAPTURE, and that property was
///     never what any of them established. It comes from one line in the
///     resolver: only a span of words may be a name.
///     </para>
///     <para>
///     So a name cannot contain a bracket or a symbol, cannot straddle one, and
///     a word beside a bracket cannot be swallowed. That is why bracket-delimited
///     and symbol-separated patterns reserve nothing, and it is the argument the
///     whole zero-glue direction is built on.
///     </para>
///     <para>
///     Widening what may be part of a name would invalidate all of it in
///     silence, and no fuzzer would report it — the resolutions would still be
///     unique, they would simply be unique and wrong. This is the test that
///     would notice, and it is here rather than in the resolver's own file
///     because what it guards is a language property, not an implementation
///     detail.
///     </para>
/// </remarks>
[Trait(nameof(Resolver), null)]
public class NameInvariant
{
    private static Resolution Resolve(string[] names, string source)
    {
        SymbolTable symbols = new();
        // «send (_)» beside «send (_) to (_)» is what makes the capture
        // profitable: the longer name lets the CHEAPER pattern match.
        symbols.WithNames(names).WithPatterns("send _", "send _ to _", "sum of _");

        return new Resolver(symbols).Resolve(source);
    }

    [Fact(DisplayName = "a name is words and nothing else")]
    public void ANameIsWordsAndNothingElse()
    {
        // The capture that R5 exists to prevent: a longer name swallows a call
        // segment, costs FEWER lookups because a name is one lookup and a call
        // is more, and wins outright. Nothing reports it — the reading is unique.
        var captured = Resolve(["hello", "alice", "hello to alice"], "send hello to alice");

        Assert.Equal("Resolved", captured.Kind.ToString());
        Assert.Equal("send «hello to alice»", captured.Reading);

        // A bracket cannot be inside a name, so the same name cannot reach
        // across one.
        var safe = Resolve(["hello", "alice", "hello to alice"], "send (hello) to alice");

        Assert.Equal("Resolved", safe.Kind.ToString());
        Assert.Equal("send ⟨«hello»⟩ to «alice»", safe.Reading);

        // ABSENT, not merely dearer, and proved by cost rather than asserted by
        // listing readings — «Readings» carries only the winning cost, so asking
        // it whether the capturing reading is there answers nothing.
        //
        // A name is ONE lookup and the call it swallows is more, so capture is
        // always the cheaper reading where it can be built at all: unbracketed
        // it wins at 2. If it could be built across a bracket it would still
        // cost 2 and would still win. The bracketed statement resolves at 4, so
        // the cheaper reading was not available to lose — it does not exist.
        //
        // This is what fails loudly if someone later lets a name span a bracket:
        // the cost drops back to 2 and the reading changes with it.
        Assert.Equal(2, captured.Cost);
        Assert.Equal(4, safe.Cost);
    }

    [Fact(DisplayName = "a symbol cannot be part of a name either")]
    public void ASymbolCannotBePartOfANameEither()
    {
        // The same invariant, and the reason a symbol separator is free: «=>»,
        // «..» and «:» can never be swallowed, whatever anyone declares.
        SymbolTable symbols = new();
        symbols.WithNames("a", "b", "a + b");

        // «a + b» is in scope as a NAME and still cannot be read as one, because
        // the span is not words
        var resolution = new Resolver(symbols).Resolve("a + b");

        Assert.Equal("Resolved", resolution.Kind.ToString());
        Assert.Equal("(«a» + «b»)", resolution.Reading);
    }

    [Fact(DisplayName = "a bracket stops a name at both ends")]
    public void ABracketStopsANameAtBothEnds()
    {
        // Neither leftward nor rightward: the word beside a bracket is
        // unswallowable from either side, which is what makes a bracket-delimited
        // hole a free shape rather than a cheaper one.
        Assert.Equal("Resolved", Resolve(["x", "sum of x"], "sum of (x)").Kind.ToString());

        Assert.Equal("sum of ⟨«x»⟩", Resolve(["x", "sum of x"], "sum of (x)").Reading);
    }
}
