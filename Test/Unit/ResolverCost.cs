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
///     </list>
///     <para>
///     A ceiling rather than a benchmark: this is a regression test, and the
///     number sits well above what it costs now and well below what it cost
///     before, so it fails on a return to the old shape and not on ordinary
///     variation. More wins remain — triangular span storage, patterns indexed by
///     first anchor word, a pooled table for repeated editor calls — and each
///     should move this number down rather than leave it where it is.
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

        // 25 MB as this is written, 158 MB before the two changes above
        Assert.True(megabytes < 60,
                    $"resolving 149 lexemes allocated {megabytes:F1} MB, past the 60 MB ceiling");
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
        symbols.Operators["^"] = new Operator(25, IsLeftAssociative: false);
        symbols.WithNames("a", "b", "c");

        Resolver added = new(symbols);

        Assert.Equal("Resolved", added.Resolve("a + b").Kind.ToString());

        // right associative, so «a ^ b ^ c» groups to the right — which is the
        // half of the recurrence that needs the «power + 1» slot
        Assert.Equal("(«a» ^ («b» ^ «c»))", added.Resolve("a ^ b ^ c").Reading);
    }
}
