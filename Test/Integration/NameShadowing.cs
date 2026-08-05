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

    [Theory(DisplayName = "pattern glue is refused inside a name, or as the whole of one")]
    [InlineData("send to me", nameof(GlueInName))]
    [InlineData("time to live", nameof(GlueInName))]
    [InlineData("to uppercase", null)]
    [InlineData("delivered to", null)]
    [InlineData("to", nameof(GlueAsName))]
    [InlineData("to to", nameof(GlueAsName))]
    [InlineData("to to to", nameof(GlueAsName))]
    public void PatternGlueIsRefusedInsideANameOrAsTheWholeOfOne(string name, string refused)
    {
        // R5′ over pattern glue, which is the half «IS-AND-EQUALITY» §4 was
        // always about: «to uppercase» becomes legal while «time to live» stays
        // refused. A name can only re-read a call it spans, and spanning one
        // needs a word on each side of the glue.
        //
        // And the SECOND clause, which the first statement of R5′ was missing: a
        // name made only of glue has none interiorly and still captures. That is
        // one rule with two arities rather than a capture rule beside a
        // legibility one — see the tie it prevents, below.
        var findings = Compilation.Of(new SourceText(
            $"var {name} => Number;\nfunction send (x => Number) to (y => Number) {{ return x; }}\n",
            "Player.ron")).Findings;

        if (refused is null)
        {
            Assert.Empty(findings);
            return;
        }

        Assert.Equal(refused, Assert.Single(findings).GetType().Name);
    }

    [Theory(DisplayName = "and the whole-glue name is what makes a call unwritable")]
    [InlineData("send to to to", false)]
    [InlineData("send to to to to", true)]
    public void AndTheWholeGlueNameIsWhatMakesACallUnwritable(string source, bool ties)
    {
        // The reason the second clause is a capture rule and not a style rule.
        // With «to» alone there is one reading at every length. Add «to to» and
        // the literal has two viable positions at the same cost, so the
        // statement cannot be written at all — and no bracketing at the call
        // site repairs a tie its own declaration created.
        //
        // FIVE tokens and not four: at four the literal can only sit in one
        // place, because putting it last leaves the second hole empty. Measured,
        // because the prediction that four would do it was wrong.
        SymbolTable one = new();
        SymbolTable two = new();

        one.WithNames("to").WithPatterns("send _ to _");
        two.WithNames("to", "to to").WithPatterns("send _ to _");

        var lexemes = Lexemes.Lex(source);

        Assert.Equal(ties ? "NoParse" : "Resolved", new Resolver(one).Resolve(lexemes).Kind.ToString());
        Assert.Equal(ties ? "Ambiguous" : "Resolved", new Resolver(two).Resolve(lexemes).Kind.ToString());
    }

    [Theory(DisplayName = "a name may not absorb the word that tells two patterns apart")]
    [InlineData("var things => Number;\nvar all things => Number;\n", true)]
    [InlineData("var all things => Number;\n", true)]
    [InlineData("var all count of items => Number;\n", true)]
    [InlineData("var things => Number;\n", false)]
    [InlineData("var some things => Number;\nvar things => Number;\n", false)]
    public void ANameMayNotAbsorbTheWordThatTellsTwoPatternsApart(string names, bool refused)
    {
        // R7b. «send (_) to all (_)» is «send (_) to (_)» with «all» at the
        // start of its second hole, so «all things» reads the whole of «send x
        // to all things» through the SHORTER pattern for what the longer one
        // costs reading it through «things». A tie at a call site, created by a
        // declaration somewhere else.
        //
        // BLANKET, and the reason is the table rather than a preference.
        // Conditioning on the remainder resolving makes legality depend on the
        // value language, which grows all session — so «var all things» would be
        // legal until someone declared «things», and then the convention refuses
        // «var things», the more natural of the two, about a variable its author
        // may not own.
        //
        // Row three is why the condition could not have been "the remainder is a
        // declared name" even if the table were stable: «count of items» is a
        // CALL, not a name, and that case is worse than the tie —
        //
        //     send x to all count of items   4 -> 3   resolved both ways
        //     send x to all things           3 -> 3   ambiguous
        //
        // — because the name is cheaper and wins with nothing reported.
        //
        // The last two rows still have to stay silent: a name that does not
        // begin with an inserted word is every ordinary name there is. Conditional is not the general answer: the
        // all-glue clause above has no condition to test, because its two
        // readings are two placements of one literal. It is the right answer
        // where the hazard has a condition, and this one does.
        const string patterns = "function send (x => Number) to (y => Number) { return x; }\n"
                              + "function send (x => Number) to all (y => Number) { return x; }\n";

        var findings = Compilation.Of(new SourceText(names + patterns, "Player.ron")).Findings;

        if (refused is false)
        {
            Assert.Empty(findings);
            return;
        }

        var finding = Assert.IsType<NameAbsorbsRefinement>(Assert.Single(findings));

        Assert.Equal("all", finding.Word);
        Assert.Equal("send (_) to (_)", finding.Refined);
        Assert.Equal("send (_) to all (_)", finding.Refining);
    }

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

    [Theory(DisplayName = "and whichever was written later is the one asked to give way")]
    [InlineData(true, "Player.ron:4:10", "Player.ron:2:5", "the name that would absorb it")]
    [InlineData(false, "Player.ron:4:5", "Player.ron:2:10", "the pattern it would absorb into")]
    public void AndWhicheverWasWrittenLaterIsTheOneAskedToGiveWay(bool namesFirst,
                                                                 string caret,
                                                                 string related,
                                                                 string label)
    {
        // Nothing in the earlier file changed, so blaming it is both wrong and
        // unactionable. Every rule here names two declarations and the caret
        // goes on the later one — which for this rule can be either side, since
        // a name can predate the pattern pair that makes it ambiguous or follow
        // it.
        const string patterns = "function send (x => Number) to (y => Number) { return x; }\n"
                              + "function send (x => Number) to all (y => Number) { return x; }\n";

        const string names = "var things => Number;\nvar all things => Number;\n";

        var reported = Diagnostics.Report(Only(namesFirst ? names + patterns : patterns + names)).Split('\n');

        Assert.StartsWith(caret + ":", reported[0]);
        Assert.Equal($"    {related}: {label}", reported[1]);
    }

    [Fact(DisplayName = "and the reading it takes can be cheaper rather than merely equal")]
    public void AndTheReadingItTakesCanBeCheaperRatherThanMerelyEqual()
    {
        // The case that decided blanket. Where the remainder is a NAME the two
        // readings cost the same and the tie is reported; where it is a CALL the
        // name is cheaper and simply wins, with nothing said.
        //
        // A condition on "the remainder is a declared name" is silent here,
        // which is the worse of the two to be silent about — and it is why the
        // condition, had one been kept, would have had to be "the remainder
        // resolves" rather than "the remainder is declared".
        SymbolTable without = new();
        SymbolTable with = new();

        without.WithNames("x", "items").WithPatterns("send _ to _", "send _ to all _", "count of _");
        with.WithNames("x", "items", "all count of items")
            .WithPatterns("send _ to _", "send _ to all _", "count of _");

        var lexemes = Lexemes.Lex("send x to all count of items");

        var alone = new Resolver(without).Resolve(lexemes);
        var absorbed = new Resolver(with).Resolve(lexemes);

        Assert.Equal("send «x» to all count of «items»", alone.Reading);
        Assert.Equal("send «x» to «all count of items»", absorbed.Reading);

        // Both resolve. Nothing reports it. The cost is the only thing that
        // moved, and a reader has no way to see that it did.
        Assert.Equal("Resolved", alone.Kind.ToString());
        Assert.Equal("Resolved", absorbed.Kind.ToString());
        Assert.True(absorbed.Cost < alone.Cost, $"{absorbed.Cost} against {alone.Cost}");
    }

    [Fact(DisplayName = "and inserting at the first hole is R6's, not this")]
    public void AndInsertingAtTheFirstHoleIsR6sNotThis()
    {
        // «sum all (_)» refines «sum (_)» the same way, and one anchor run then
        // begins the other — so the pattern pair is refused before a name is
        // looked at. What is left for R7b is insertion at a LATER hole, where
        // the anchors are equal and R6 has nothing to say.
        //
        // WITH A NAME in scope, which is what the audit found missing: this
        // asserted on the patterns alone, so R7b firing as well was invisible.
        // One structural mistake grew into one finding per name beginning
        // «all», every one of them with the same repair — fix the pattern pair.
        var findings = Compilation.Of(new SourceText(
            "var all things => Number;\n"
          + "function sum (x => Number) { return x; }\n"
          + "function sum all (x => Number) { return x; }\n",
            "Player.ron")).Findings;

        Assert.Equal(FindingKind.AnchorPrefix, Assert.Single(findings).Kind);
    }

    [Fact(DisplayName = "and the repair asks for whichever declaration the caret is on")]
    public void AndTheRepairAsksForWhicheverDeclarationTheCaretIsOn()
    {
        // Found by audit. The sentence said «all things» cannot be declared
        // whichever declaration was later, so a caret on the PATTERN arrived
        // with a message blaming the name — sending someone to change the
        // earlier of the two, against the convention every other rule follows.
        const string patterns = "function send (x => Number) to (y => Number) { return x; }\n"
                              + "function send (x => Number) to all (y => Number) { return x; }\n";

        const string names = "var things => Number;\nvar all things => Number;\n";

        Assert.Contains("«send (_) to all (_)» cannot be declared while «all things» is",
                        Only(names + patterns).Message);

        Assert.Contains("«all things» cannot be declared while", Only(patterns + names).Message);
    }

    [Fact(DisplayName = "and it does not claim the two readings cost the same")]
    public void AndItDoesNotClaimTheTwoReadingsCostTheSame()
        // They do when the remainder is a name and they do not when it is a
        // call — where the absorbing reading is CHEAPER and wins outright. The
        // message said "the same" unconditionally, denying the stronger of the
        // two reasons the rule exists, and no test rendered it.
        => Assert.Contains("for no more than the intended reading costs, and sometimes for less",
                           Only("var things => Number;\nvar all things => Number;\n"
                              + "function send (x => Number) to (y => Number) { return x; }\n"
                              + "function send (x => Number) to all (y => Number) { return x; }\n").Message);
}
