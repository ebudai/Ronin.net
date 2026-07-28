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

        // 15 MB as this is written. It was 158 MB before the binding-power and
        // lazy-collection work, and 22 MB before the table went triangular — so
        // this ceiling catches losing any of the three.
        Assert.True(megabytes < 20,
                    $"resolving 149 lexemes allocated {megabytes:F1} MB, past the 20 MB ceiling");
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

    [Fact(DisplayName = "only the binding powers something asks for get a slot")]
    public void OnlyTheBindingPowersSomethingAsksForGetASlot()
    {
        // Six levels: zero, the pattern binding power, and each operator's own
        // power and one above it. «E[i, j, 13]» is reachable only if an operator
        // binds at 13, and the table carried it regardless.
        SymbolTable symbols = new();

        Assert.Equal([10, 20], symbols.Operators.Values.Select(op => op.BindingPower).Distinct().Order());

        // An operator added at a new level has to widen the table with it.
        // Hard-coding six would leave every statement using the new operator
        // silently unresolvable, which is why the resolver reads the operator
        // table rather than a constant — so this asserts the derivation, not the
        // number.
        symbols.Operators["^"] = new Operator(25, Ronin.Runtime.Builtin.Lift(
            (left, right) => System.Math.Pow((double)left, (double)right)), IsLeftAssociative: false);
        symbols.WithNames("a", "b", "c");

        Resolver added = new(symbols);

        Assert.Equal("Resolved", added.Resolve("a + b").Kind.ToString());

        // right associative, so «a ^ b ^ c» groups to the right — which is the
        // half of the recurrence that needs the «power + 1» slot
        Assert.Equal("(«a» ^ («b» ^ «c»))", added.Resolve("a ^ b ^ c").Reading);
    }
}
