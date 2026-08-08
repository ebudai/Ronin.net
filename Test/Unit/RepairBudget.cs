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
}
