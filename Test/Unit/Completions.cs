// Copyright © 2026 Eric Budai

using Ronin.Compiler;

namespace Unit;

/// <summary>
///     What the writer is offered while a statement is still being typed.
/// </summary>
[Trait(nameof(Resolver), null)]
public class Completions
{
    private static Completion Scope(string[] names, string[] patterns)
    {
        SymbolTable symbols = new();
        symbols.WithNames(names).WithPatterns(patterns);
        return new Completion(symbols);
    }

    private static IReadOnlyList<Candidate> After(Completion completion, string typed)
        => completion.After(Lexemes.Lex(typed));

    [Fact(DisplayName = "continuing a name outranks starting one")]
    public void ContinuingANameOutranksStartingOne()
    {
        var completion = Scope(["cash on hand", "cash flow", "debt"], []);

        var candidates = After(completion, "cash on");

        // «hand» continues two typed words; the rest continue none and are the
        // "or start something else" tail
        Assert.Equal("hand", candidates[0].Word);
        Assert.Equal("cash on hand", candidates[0].Whole);
        Assert.Equal(2, candidates[0].Matched);

        Assert.All(candidates.Skip(1), candidate => Assert.Equal(0, candidate.Matched));
        Assert.Equal(["cash", "debt"], candidates.Skip(1).Select(candidate => candidate.Word).Distinct());
    }

    [Fact(DisplayName = "a name and a pattern can both continue")]
    public void ANameAndAPatternCanBothContinue()
    {
        var completion = Scope(["sum total"], ["sum of _"]);

        var candidates = After(completion, "sum");

        Assert.Equal(
            [(CandidateKind.Pattern, "of"), (CandidateKind.Name, "total")],
            candidates.Where(candidate => candidate.Matched is 1)
                      .Select(candidate => (candidate.Kind, candidate.Word)));
    }

    [Fact(DisplayName = "an empty statement offers every opening word")]
    public void AnEmptyStatementOffersEveryOpeningWord()
    {
        var completion = Scope(["base price", "tax"], ["compute total for _"]);

        var candidates = After(completion, string.Empty);

        // nothing is being continued, so the whole scope is on offer, longest
        // first: a three word anchor, a two word name, then a one word name
        Assert.All(candidates, candidate => Assert.Equal(0, candidate.Matched));
        Assert.Equal(["compute", "base", "tax"], candidates.Select(candidate => candidate.Word));
        Assert.Equal([3, 2, 1], candidates.Select(candidate => candidate.Words));
    }

    [Fact(DisplayName = "the greedier reading is offered first")]
    public void TheGreedierReadingIsOfferedFirst()
    {
        // «cash on hand» costs one lookup where «cash» «on hand» costs two, so
        // the resolver takes the longer name. The list is ordered to match, or it
        // teaches the writer to expect the opposite of what they will get.
        var completion = Scope(["cash", "cash flow", "cash on hand"], []);

        var candidates = After(completion, "cash");

        Assert.Equal(
            [("on", "cash on hand"), ("flow", "cash flow")],
            candidates.Where(candidate => candidate.Matched is 1)
                      .Select(candidate => (candidate.Word, candidate.Whole)));
    }

    [Fact(DisplayName = "a symbol ends the run")]
    public void ASymbolEndsTheRun()
    {
        // «base price + base» is partway through a second «base price», not
        // through some four-word name, because nothing spanning an operator can
        // be one name
        var completion = Scope(["base price", "tax"], []);

        var candidates = After(completion, "base price + base");

        Assert.Equal("price", candidates[0].Word);
        Assert.Equal(1, candidates[0].Matched);
    }

    [Fact(DisplayName = "a bracket ends the run too")]
    public void ABracketEndsTheRunToo()
    {
        var completion = Scope(["base price"], []);

        // the run is «base», not «compute base», so «price» continues one word
        // and never two however many words precede the bracket
        var candidates = After(completion, "compute (base");
        Assert.Equal("price", candidates[0].Word);
        Assert.Equal(1, candidates[0].Matched);

        Assert.All(After(completion, "compute ("), candidate => Assert.Equal(0, candidate.Matched));
    }

    [Fact(DisplayName = "an earlier word may already be spoken for")]
    public void AnEarlierWordMayAlreadyBeSpokenFor()
    {
        // after «send hello», «hello» is a finished argument to «send _» and also
        // a prefix of the name «hello to alice», so «to» has to be offered
        var completion = Scope(["alice", "hello", "hello to alice"], ["send _", "send _ to _"]);

        var candidates = After(completion, "send hello");

        Assert.Contains(candidates, candidate => candidate.Word is "to" && candidate.Whole is "hello to alice");
    }

    [Fact(DisplayName = "a finished name offers nothing further of itself")]
    public void AFinishedNameOffersNothingFurtherOfItself()
    {
        var completion = Scope(["tax"], []);

        var candidates = After(completion, "tax");

        // «tax» is complete, so the only thing left to do is start another one
        Assert.All(candidates, candidate => Assert.Equal(0, candidate.Matched));
        Assert.Equal(["tax"], candidates.Select(candidate => candidate.Word));
    }

    [Fact(DisplayName = "the same word from two sources is offered once each")]
    public void TheSameWordFromTwoSourcesIsOfferedOnceEach()
    {
        var completion = Scope(["of list"], ["sum of _"]);

        var candidates = After(completion, "sum of");

        // «of» opens the name «of list» and also sits inside the pattern anchor,
        // but only the name can still be continued from here
        Assert.Single(candidates, candidate => candidate.Word is "list");
    }

    [Fact(DisplayName = "an empty scope offers nothing")]
    public void AnEmptyScopeOffersNothing()
    {
        Assert.Empty(After(Scope([], []), "anything at all"));
    }

    [Fact(DisplayName = "completion rejects nonsense")]
    public void CompletionRejectsNonsense()
    {
        Assert.Throws<ArgumentNullException>(() => new Completion(null));
        Assert.Throws<ArgumentNullException>(() => Scope([], []).After(null));
    }
}
