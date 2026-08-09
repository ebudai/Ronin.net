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

    [Fact(DisplayName = "and it stops mid-search, not only between readings")]
    public void AndItStopsMidSearchNotOnlyBetweenReadings()
    {
        // A reading of two ambiguous children needs a bracket around each, so its
        // search fails every single-span candidate before it reaches a pair. The
        // budget can run out among those singles — inside one reading's search
        // rather than cleanly between readings — and the guard that stops it is
        // per candidate, not only per size, so a search already under way stops
        // where it stands instead of finishing the size it was on.
        SymbolTable symbols = new();
        symbols.WithNames("a", "b", "a to b").WithPatterns("send _", "send _ to _");

        var lexemes = Lexemes.Lex("( send a to b ) + ( send a to b )");
        var ambiguity = new Resolver(symbols).Resolve(lexemes);

        Assert.Equal(4, ambiguity.Readings.Count);

        // One resolution buys no repair: every reading needs a pair, and the one
        // look is spent on a single that cannot select — but the search stops
        // among the singles rather than running the size out.
        Assert.Empty(Repairs.For(new Resolver(symbols), lexemes, ambiguity, budget: 1));

        // Given enough, each reading is repaired with its two pairs.
        var repairs = Repairs.For(new Resolver(symbols), lexemes, ambiguity, budget: 4000);

        Assert.Equal(4, repairs.Count);
        Assert.All(repairs, repair => Assert.Equal(4, repair.Insertions.Count));
    }
}
