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

    [Fact(DisplayName = "and a pattern with glue is not asked, because R5 asked already")]
    public void AndAPatternWithGlueIsNotAskedBecauseR5AskedAlready()
    {
        // To reach the whole of «send (_) to (_)» a name has to span «to», and
        // R5 refuses any name containing glue. Asking both would be two findings
        // for one repair.
        const string glued = "function send (x => Number) to (y => Number) { return x; }\n";

        Assert.Equal(FindingKind.GlueInName, Only("var send to me => Number;\n" + glued).Kind);
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

    [Theory(DisplayName = "an operator word is refused inside a name and nowhere else")]
    [InlineData("total otherwise zero", true)]
    [InlineData("a otherwise b otherwise c", true)]
    [InlineData("otherwise valid", false)]
    [InlineData("valid otherwise", false)]
    [InlineData("otherwise", false)]
    [InlineData("otherwise otherwise", false)]
    public void AnOperatorWordIsRefusedInsideANameAndNowhereElse(string name, bool refused)
    {
        // R5′. An infix reading needs an operand on EACH SIDE, so a name can
        // only be its rival where the word has words on both sides of it. The
        // blanket form refused every name containing one — which is «is valid»,
        // «to uppercase», «not found», the shape a spaces-in-names grammar most
        // encourages — for a rivalry those names cannot enter.
        //
        // «otherwise otherwise» is two words and therefore has no interior at
        // all, which follows from the rule rather than being an exception to it.
        var findings = Compilation.Of(new SourceText($"var {name} => Number;\n", "Player.ron")).Findings;

        if (refused is false)
        {
            Assert.Empty(findings);
            return;
        }

        var finding = Assert.IsType<InfixInName>(Assert.Single(findings));

        Assert.Equal(name, finding.Name);
        Assert.Equal("otherwise", finding.Word);
    }

    [Fact(DisplayName = "and a pattern is still refused for using one anywhere")]
    public void AndAPatternIsStillRefusedForUsingOneAnywhere()
        // The narrowing is about NAMES. A name competes with the infix reading
        // and needs an operand each side to do it; a pattern using the word is
        // the other failure — it costs exactly what the operation costs and ties
        // — and that is true wherever the word sits.
        => Assert.Equal(FindingKind.InfixInPattern,
                        Only("function otherwise (value => Number) { return value; }\n").Kind);
}
