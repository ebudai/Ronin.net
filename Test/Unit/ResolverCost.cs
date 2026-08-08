// Copyright © 2026 Eric Budai

using Ronin.Compiler;

namespace Unit;

/// <summary>
///     What resolving a statement costs, held to a ceiling.
/// </summary>
///
/// <remarks>
///     <para>
///     The table is cubic in the token count — two <c>(n+1)²</c> tables and one
///     <c>(n+1)² × levels</c> — so the cost of getting this wrong is not
///     proportional to the mistake. Resolving a statement of 299 lexemes
///     allocated 766 MB, and two changes took it to 126 MB:
///     </para>
///     <list type="bullet">
///         <item>
///             index only the minimum binding powers the recurrences can ask
///             for, which is six and not thirty-two — and derive them from the
///             operator table, so an operator added at a new level cannot index a
///             slot that is not there
///         </item>
///         <item>allocate a cell's collections on first offer rather than at construction</item>
///         <item>
///             store spans triangularly: a span runs from «i» to «j» with
///             i &lt;= j, so half of a rectangular table is spans that cannot
///             exist and the largest table paid for that half once per binding
///             power
///         </item>
///         <item>
///             index patterns by their first word, so a span asks only the
///             patterns that could begin at it rather than all of them
///         </item>
///     </list>
///     <para>
///     A ceiling rather than a benchmark: this is a regression test, and the
///     number sits well above what it costs now and well below what it cost
///     before, so it fails on a return to the old shape and not on ordinary
///     variation. What remains is a pooled table for repeated editor calls, which
///     is a lifetime question rather than a shape one.
///     </para>
/// </remarks>
[Trait(nameof(Resolver), null)]
public class ResolverCost
{
    /// <summary>
    ///     The resolver's allocation budget, in megabytes.
    /// </summary>
    ///
    /// <remarks>
    ///     ONE constant, used by the comparison and by the message. They were two
    ///     numbers and the raise moved only one, so a regression between them
    ///     would have failed while quoting a limit nothing enforced.
    /// </remarks>
    private const int Ceiling = 20;

    [Fact(DisplayName = "resolving stays within its allocation budget")]
    public void ResolvingStaysWithinItsAllocationBudget()
    {
        SymbolTable symbols = new();
        symbols.WithNames("base price", "tax").WithPatterns("compute total for _", "send _ to _");

        Resolver resolver = new(symbols);

        var lexemes = Lexemes.Lex(string.Join(" + ", Enumerable.Repeat("base price", 50)));

        Assert.Equal(149, lexemes.Count);

        // the first call JITs and warms; the measurement is of the second
        Assert.Equal("Resolved", resolver.Resolve(lexemes).Kind.ToString());

        var before = GC.GetAllocatedBytesForCurrentThread();
        resolver.Resolve(lexemes);
        var megabytes = (GC.GetAllocatedBytesForCurrentThread() - before) / 1024.0 / 1024.0;

        // 11.3 MB as this is written, and this comment said 26.2 until someone
        // measured it. Nothing had gone wrong: the witness machinery was deleted
        // when parents began enumerating their children, and that took more than
        // half the allocation with it. Nobody re-ran the number, so a stale
        // figure sat here with a ceiling of 32 above a program using 11.
        //
        // WHICH MATTERED, because the stale figure was quoted into a design
        // decision. It said a binding-power level costs about 4.5 MB, and that
        // number was used to argue against a ladder of eight. Measured on this
        // statement with levels added:
        //
        //     none  11.3      two  12.1      four  13.5      eight  16.3
        //
        // About 0.6 MB a level. An eight-rung ladder is about 5 MB, not the 36
        // the old figure implied, and the price was never a reason to choose a
        // smaller number. A measurement written down is a measurement that stops
        // being true; this one is now the thing the ceiling is set from rather
        // than a sentence beside it.
        //
        // It was 158 MB before the binding-power and lazy-collection work, and 22
        // before the table went triangular. 20 leaves room for the ladder above
        // and still fails on losing any of those.
        //
        // The next thing to move this has to say what it did to the margin, and
        // to re-measure rather than quote — which is what was skipped.
        Assert.True(megabytes < Ceiling,
                    $"resolving 149 lexemes allocated {megabytes:F1} MB, past the {Ceiling} MB ceiling");
    }

    [Fact(DisplayName = "adjacent free holes do not enumerate every way to split them")]
    public void AdjacentFreeHolesDoNotEnumerateEveryWayToSplitThem()
    {
        // Found by audit, and it was a hang rather than a slow path. A pattern of
        // h adjacent holes over n words has «C(n-1, h-1)» ways to split, and the
        // matcher yielded every one, combined with every argument alternative,
        // before anything trimmed them — so a twenty-five-lexeme statement took
        // twelve SECONDS to resolve and a thirty-lexeme one did not finish.
        //
        // Keeping the cheapest «Most» fillings per subproblem, memoised, makes it
        // polynomial: the cheapest few of a whole are built from the cheapest few
        // of its parts, because cost is additive. This is the same top-K property
        // the cell already relied on, applied to the recursion that feeds it.
        SymbolTable symbols = new();
        symbols.WithPatterns("p " + string.Join(" ", Enumerable.Repeat("_", 12)));
        for (var n = 1; n <= 24; ++n) symbols.WithNames(string.Join(" ", Enumerable.Repeat("x", n)));

        var lexemes = Lexemes.Lex("p " + string.Join(" ", Enumerable.Repeat("x", 24)));

        Assert.Equal(25, lexemes.Count);

        Resolver resolver = new(symbols);
        resolver.Resolve(lexemes);

        var before = GC.GetAllocatedBytesForCurrentThread();
        resolver.Resolve(lexemes);
        var megabytes = (GC.GetAllocatedBytesForCurrentThread() - before) / 1024.0 / 1024.0;

        // 10 MB as this is written. The number is not the point — its GROWTH is:
        // exponential enumeration allocated a Filling per split-combination, which
        // for this case is «C(23, 11)» ≈ 1.3 million and reached gigabytes. A
        // ceiling of 30 has three times the margin and still fails by two orders
        // of magnitude on a return to enumerating them all.
        Assert.True(megabytes < 30,
                    $"resolving twelve adjacent holes over 24 words allocated {megabytes:F1} MB — the split enumeration is back");
    }

    [Fact(DisplayName = "and a bound below one hole is carried up like a bound below the pattern")]
    public void AndABoundBelowOneHoleIsCarriedUpLikeABoundBelowThePattern()
    {
        // The count a matcher reports is a floor when a part was capped, and a
        // part is either a completion of the remaining holes or an ARGUMENT of
        // one hole. The completion case rides every deep resolution; the argument
        // case needs an argument that is itself ambiguous past the cap, which is
        // a call inside a call.
        //
        // «wrap _ up» forces its hole to span exactly the middle, and that middle
        // is a three-hole call over eight words — twenty-one readings, more than
        // are kept. So the argument cell is bounded, and «wrap» is bounded
        // because its argument is.
        SymbolTable symbols = new();
        symbols.WithPatterns("wrap _ up", "p " + string.Join(" ", Enumerable.Repeat("_", 3)));
        for (var n = 1; n <= 8; ++n) symbols.WithNames(string.Join(" ", Enumerable.Repeat("x", n)));

        var resolution = new Resolver(symbols).Resolve(
            "wrap p " + string.Join(" ", Enumerable.Repeat("x", 8)) + " up");

        Assert.Equal("Ambiguous", resolution.Kind.ToString());
        Assert.True(resolution.Bounded);
        Assert.True(resolution.Total > Resolver.Kept);
    }

    [Fact(DisplayName = "a statement past the ceiling is refused, not resolved slowly")]
    public void AStatementPastTheCeilingIsRefusedNotResolvedSlowly()
    {
        // Cubic in the lexeme count, so one generated or pasted statement can ask
        // for arbitrarily much of the table. Per-statement resolution bounds the
        // ordinary case and not that one.
        SymbolTable symbols = new();
        symbols.WithNames("a");

        Resolver resolver = new(symbols);

        // n names with n-1 operators between them, so 2n-1 lexemes — which
        // straddles the ceiling rather than landing on it
        var within = Lexemes.Lex(string.Join(" + ", Enumerable.Repeat("a", Resolver.MaxLexemes / 2)));
        var past = Lexemes.Lex(string.Join(" + ", Enumerable.Repeat("a", (Resolver.MaxLexemes / 2) + 1)));

        Assert.Equal(Resolver.MaxLexemes - 1, within.Count);
        Assert.Equal(Resolver.MaxLexemes + 1, past.Count);

        Assert.Equal("Resolved", resolver.Resolve(within).Kind.ToString());

        var refused = resolver.Resolve(past);

        // distinct from a failure to parse: the statement may be perfectly good
        // and nothing here found out
        Assert.Equal("TooLong", refused.Kind.ToString());
        Assert.Contains("split it", refused.ToString());
    }

    [Fact(DisplayName = "a pattern has a width, because matching recurses over it")]
    public void APatternHasAWidthBecauseMatchingRecursesOverIt()
    {
        // Match recurses one frame per segment, and nothing else bounds that: a
        // pattern's width comes from a declaration, which the statement ceiling
        // does not constrain.
        var widest = Pattern.Parse(string.Join(' ', Enumerable.Repeat("word", Pattern.MaxSegments)));

        Assert.Equal(Pattern.MaxSegments, widest.Segments.Count);

        var wider = string.Join(' ', Enumerable.Repeat("word", Pattern.MaxSegments + 1));

        var refused = Assert.Throws<ArgumentException>(() => Pattern.Parse(wider));

        Assert.Contains("at most", refused.Message);
    }

    [Fact(DisplayName = "an operator is refused where it is written, not where it is used")]
    public void AnOperatorIsRefusedWhereItIsWrittenNotWhereItIsUsed()
    {
        // The table is mutable so a scope can add an operator, and every invalid
        // entry failed far from the insertion that caused it: a binding power
        // outside the indexed range came back as a raw IndexOutOfRangeException
        // while CONSTRUCTING a resolver, and a null implementation resolved
        // perfectly well and then threw inside the evaluator.
        object apply(object left, object right) => left;

        foreach (var power in (int[])[-1, Resolver.MaxBindingPower + 1, int.MaxValue])
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Operator(power, apply));
        }

        Assert.Throws<ArgumentNullException>(() => new Operator(10, null));

        // and the range's own edges are legal
        Assert.Equal(0, new Operator(0, apply).BindingPower);
        Assert.Equal(Resolver.MaxBindingPower, new Operator(Resolver.MaxBindingPower, apply).BindingPower);
    }

    [Fact(DisplayName = "only the binding powers something asks for get a slot")]
    public void OnlyTheBindingPowersSomethingAsksForGetASlot()
    {
        // Zero, the pattern binding power, and each operator's own power and one
        // above it. «E[i, j, 13]» is reachable only if an operator binds at 13,
        // and the table carried it regardless.
        //
        // The list is the operator table's, so an operator added at a new level
        // shows up here — «is» at 5 did — and the cost of that level shows up in
        // the budget above.
        SymbolTable symbols = new();

        Assert.Equal([5, 6, 10, 20, 21], symbols.Operators.Values.Select(op => op.BindingPower).Distinct().Order());

        // An operator added at a new level has to widen the table with it.
        // Hard-coding six would leave every statement using the new operator
        // silently unresolvable, which is why the resolver reads the operator
        // table rather than a constant — so this asserts the derivation, not the
        // number.
        symbols.Operators["^"] = new Operator(25, Ronin.Runtime.Builtin.Lift(
            (left, right) => System.Math.Pow((double)left, (double)right)), isLeftAssociative: false);
        symbols.WithNames("a", "b", "c");

        Resolver added = new(symbols);

        Assert.Equal("Resolved", added.Resolve("a + b").Kind.ToString());

        // right associative, so «a ^ b ^ c» groups to the right — which is the
        // half of the recurrence that needs the «power + 1» slot
        Assert.Equal("(«a» ^ («b» ^ «c»))", added.Resolve("a ^ b ^ c").Reading);
    }

    /// <summary>
    ///     The least a list of these values can cost: one array, one object.
    /// </summary>
    ///
    /// <remarks>
    ///     A test-only oracle, and it exists because the obvious baseline was
    ///     the implementation. Comparing «Owned.Of» with «Owned.Of» measures
    ///     nothing about how «Owned.Of» is written.
    /// </remarks>
    private sealed class Barely(string[] values)
    {
        public static Barely Two(string first, string second) => new([first, second]);

        public static Barely Mapping(IReadOnlyList<Node> order)
        {
            var made = new string[order.Count];

            for (var at = 0; at < made.Length; ++at) made[at] = order[at].ToString();

            return new(made);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public override string ToString() => string.Join(", ", values);
    }
}
