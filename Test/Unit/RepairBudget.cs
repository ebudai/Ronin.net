// Copyright © 2026 Eric Budai

using Ronin.Compiler;

namespace Unit;

/// <summary>
///     The repair search stops spending past its budget.
/// </summary>
///
/// <remarks>
///     The search resolves a candidate bracketing per tree span, and pairs of
///     them where a single fails. A statement near the lexeme limit with every
///     single failing can ask for more resolutions than are worth spending on
///     one error — so past a budget the search stops, and the reading is reported
///     without a repair rather than the editor hanging on a keystroke. A hang is
///     the one outcome worse than an error that offers no fix.
/// </remarks>
[Trait(nameof(Repairs), null)]
public class RepairBudget
{
    /// <summary>The repair search's allocation ceiling for the reproduction, in megabytes.</summary>
    ///
    /// <remarks>
    ///     A regression tripwire, not a tight bound: it separates the O(nodes)
    ///     shape the search has now from the O(2ⁿ) one it had, and sits far below
    ///     the six gigabytes that one spent on this source and comfortably above
    ///     the resolver's own per-candidate cost.
    /// </remarks>
    private const int Ceiling = 300;

    [Fact(DisplayName = "repairing many ambiguous children costs a resolution per node, not per subset")]
    public void RepairingManyAmbiguousChildrenCostsAResolutionPerNodeNotPerSubset()
    {
        // Found by audit. Six independently ambiguous children — sixty-four
        // readings — each needing a bracket around every child. Enumerating the
        // spans' subsets reached that six-bracket set only past every smaller set
        // that fails first, which is O(2ⁿ): the audit measured over four seconds
        // and six gigabytes on this very source, and it offered five repairs it
        // was about to stop being able to. Bracketing the whole tree once and
        // trimming is a resolution per node.
        SymbolTable symbols = new();
        symbols.WithNames("a", "b", "a to b").WithPatterns("send _", "send _ to _");

        var lexemes = Lexemes.Lex(string.Join(" + ", Enumerable.Repeat("( send a to b )", 6)));
        var ambiguity = new Resolver(symbols).Resolve(lexemes);

        // the first call JITs and warms; the measurement is of the second
        Assert.Equal(Resolver.Kept, Repairs.For(new Resolver(symbols), lexemes, ambiguity).Count);

        var before = GC.GetAllocatedBytesForCurrentThread();
        Repairs.For(new Resolver(symbols), lexemes, ambiguity);
        var megabytes = (GC.GetAllocatedBytesForCurrentThread() - before) / 1024.0 / 1024.0;

        Assert.True(megabytes < Ceiling,
                    $"repairing six ambiguous children allocated {megabytes:F0} MB, over the {Ceiling} MB ceiling");
    }

    [Fact(DisplayName = "past the budget, a reading is reported without a repair")]
    public void PastTheBudgetAReadingIsReportedWithoutARepair()
    {
        SymbolTable symbols = new();
        symbols.WithNames("a", "b", "a to b").WithPatterns("send _", "send _ to _");

        var lexemes = Lexemes.Lex("send a to b");
        var ambiguity = new Resolver(symbols).Resolve(lexemes);

        // A budget of zero: nothing may be resolved, so no candidate can be
        // verified and every reading comes back with no repair — the readings
        // are the finding's, unchanged.
        var starved = Repairs.For(new Resolver(symbols), lexemes, ambiguity, budget: 0);

        Assert.Equal(2, ambiguity.Readings.Count);
        Assert.Empty(starved);

        // And with budget to spare, the same statement is fully repaired — so it
        // is the budget that stopped it, not the statement that had none.
        Assert.Equal(2, Repairs.For(new Resolver(symbols), lexemes, ambiguity, budget: 100).Count);
    }

    [Fact(DisplayName = "and it stops the trim mid-reading, keeping a fuller repair")]
    public void AndItStopsTheTrimMidReadingKeepingAFullerRepair()
    {
        // The search verifies one full bracketing per reading and then trims it,
        // so the budget can run out inside a reading's trim as well as cleanly
        // between readings. The guard that stops it is per candidate: a reading
        // whose trim is cut short keeps the extra brackets — a fuller repair,
        // never a wrong one — and a reading it cannot even verify is dropped.
        SymbolTable symbols = new();
        symbols.WithNames("a", "b", "a to b").WithPatterns("send _", "send _ to _");

        var lexemes = Lexemes.Lex("( send a to b ) + ( send a to b )");
        var ambiguity = new Resolver(symbols).Resolve(lexemes);

        Assert.Equal(4, ambiguity.Readings.Count);

        // Enough to verify a reading or two but not to trim and reach them all —
        // fewer repairs than the full search, and every one still applies.
        var starved = Repairs.For(new Resolver(symbols), lexemes, ambiguity, budget: 2);

        Assert.InRange(starved.Count, 1, 3);

        // Given enough, every reading is repaired, and trimmed to its two pairs.
        var full = Repairs.For(new Resolver(symbols), lexemes, ambiguity, budget: 4000);

        Assert.Equal(4, full.Count);
        Assert.All(full, repair => Assert.Equal(4, repair.Insertions.Count));
    }
}
