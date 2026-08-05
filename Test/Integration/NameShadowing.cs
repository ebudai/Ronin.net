// Copyright © 2026 Eric Budai

using Ronin.Compiler;

namespace Integration;

/// <summary>
///     R6b — a name that begins with every word of a pattern would be read
///     instead of that pattern's call, and more cheaply.
/// </summary>
///
/// <remarks>
///     Cheaper is the whole problem. A tie can be reported and answered with a
///     bracket; a strictly cheaper reading wins outright and nothing says so, so
///     the text «print job» stops meaning the call and starts meaning the name
///     with no diagnostic between the two.
/// </remarks>
[Trait(nameof(Rules), null)]
public class NameShadowing
{
    private static Finding Only(string source)
        => Assert.Single(Compilation.Of(new SourceText(source, "Player.ron")).Findings);

    private const string Pattern = "function print (x => Number) { return x; }\n";
    private const string Name = "var print job => Number;\n";

    [Theory(DisplayName = "whichever was written later is the one asked to give way")]
    [InlineData(true)]
    [InlineData(false)]
    public void WhicheverWasWrittenLaterIsTheOneAskedToGiveWay(bool nameFirst)
    {
        // The caret goes on the later declaration, because that is the one whose
        // author can act: an inner pattern can invalidate a name from an
        // enclosing scope, and blaming the outer file is both wrong and
        // unactionable — nothing in it changed.
        var finding = Assert.IsType<NameShadowsPattern>(Only(nameFirst ? Name + Pattern : Pattern + Name));

        Assert.Equal("print job", finding.Name);
        Assert.Equal("print (_)", finding.Pattern);

        // line 1 is whichever came first, so the blame lands on line 2 either way
        Assert.StartsWith("Player.ron:2:", finding.Primary.ToString());
        Assert.StartsWith("Player.ron:1:", Assert.Single(finding.Related).Span.ToString());
    }

    [Fact(DisplayName = "and a name equal to the pattern's words is left alone")]
    public void AndANameEqualToThePatternsWordsIsLeftAlone()
    {
        // PROPER prefix. «print» cannot swallow the call, because the argument
        // would then have to sit beside it as a second juxtaposed name and that
        // is not an expression. Refusing it would be refusing something that
        // cannot go wrong.
        Assert.Empty(Compilation.Of(new SourceText("var print => Number;\n" + Pattern, "Player.ron")).Findings);
    }

    [Fact(DisplayName = "and a pattern may not use an operator word either")]
    public void AndAPatternMayNotUseAnOperatorWordEither()
    {
        // Found by audit. Reserving «otherwise» inside NAMES closed the capture
        // that motivated the rule and left the mirror open: a pattern using the
        // word costs exactly what the operation costs, so «x otherwise y» became
        // a TIE rather than a wrong answer.
        //
        // Refused at the declaration, because an ambiguity is reported at every
        // call site and none of them is where the mistake was made.
        var finding = Assert.IsType<InfixInPattern>(
            Only("function x otherwise (value => Number) { return value; }\n"));

        Assert.Equal("x otherwise (_)", finding.Pattern);
        Assert.Equal("otherwise", finding.Word);
    }

    [Fact(DisplayName = "and nothing is refused where the pattern is not in scope")]
    public void AndNothingIsRefusedWhereThePatternIsNotInScope()
        // The rule is about a collision, so with nothing to collide with there
        // is no reservation — «print job» is an ordinary name in a file that
        // never mentions «print (_)».
        => Assert.Empty(Compilation.Of(new SourceText(Name, "Player.ron")).Findings);

    [Fact(DisplayName = "and a pattern is still refused for using one anywhere")]
    public void AndAPatternIsStillRefusedForUsingOneAnywhere()
        // The narrowing is about NAMES. A name competes with the infix reading
        // and needs an operand each side to do it; a pattern using the word is
        // the other failure — it costs exactly what the operation costs and ties
        // — and that is true wherever the word sits.
        => Assert.Equal(FindingKind.InfixInPattern,
                        Only("function otherwise (value => Number) { return value; }\n").Kind);

    [Fact(DisplayName = "and the tie it prevents is real at the call site")]
    public void AndTheTieItPreventsIsRealAtTheCallSite()
    {
        // Why the rule exists, rather than that it fires. Without «all things»
        // the call resolves; with it, two readings cost the same and the
        // statement cannot be written — and no bracketing at the call repairs a
        // tie its own declaration created.
        SymbolTable without = new();
        SymbolTable with = new();

        without.WithNames("x", "things").WithPatterns("send _ to _", "send _ to all _");
        with.WithNames("x", "things", "all things").WithPatterns("send _ to _", "send _ to all _");

        var lexemes = Lexemes.Lex("send x to all things");

        Assert.Equal("Resolved", new Resolver(without).Resolve(lexemes).Kind.ToString());
        Assert.Equal("Ambiguous", new Resolver(with).Resolve(lexemes).Kind.ToString());
    }

    [Fact(DisplayName = "a name the compiler generates may shadow a pattern too")]
    public void ANameTheCompilerGeneratesMayShadowAPatternToo()
    {
        // Found by audit. Generated names were skipped here, and the skip's own
        // words were "not the programmer's to rename, and its origin is reported
        // already" — true of the rename and false of the reporting. Nothing else
        // reported this, so declaring «index of (_)» beside ANY loop meant the
        // counter that loop generates shadowed the call, more cheaply, with
        // nothing refused:
        //
        //     for each bank in banks { return index of bank; }
        //         with «index of (_)» declared  ->  the generated counter
        //         without it                    ->  the call
        //
        // The PATTERN is blamed whichever was written first, which is the one
        // place this rule departs from "the later declaration gives way". Both
        // authors are innocent of a name neither of them spelled, so what is
        // left to change is the pattern — and the message says so rather than
        // asking for a rename nobody can perform.
        var finding = Assert.IsType<NameShadowsPattern>(Only(
            "function index of (x => Number) { return x; }\n"
          + "var banks => Number;\n"
          + "for each bank in banks { return index of bank; }\n"));

        Assert.Equal("index of bank", finding.Name);
        Assert.Equal("index of (_)", finding.Pattern);
        Assert.Equal("bank", finding.InjectedBy);
        Assert.Contains("the generated name is not yours to change", finding.Message);

        // The CARET on the pattern, which is line 1 here — earlier than the loop
        // on line 3, and blamed anyway. Asserting the message alone leaves the
        // span free to point at the loop while the sentence asks for the
        // pattern, which is the mismatch a previous round found on another rule.
        Assert.StartsWith("Player.ron:1:10:", Diagnostics.Report(finding));
    }

    [Fact(DisplayName = "and an ordinary loop still generates nothing to complain about")]
    public void AndAnOrdinaryLoopStillGeneratesNothingToComplainAbout()
        // The rule is about a collision, so with no «index of» pattern in scope
        // the counter is an ordinary generated name and says nothing.
        => Assert.Empty(Compilation.Of(new SourceText(
            "var banks => Number;\nfor each bank in banks { return bank; }\n", "Player.ron")).Findings);
}
