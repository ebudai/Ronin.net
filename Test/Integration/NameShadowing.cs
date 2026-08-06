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
        => Assert.Single(All(source));

    private static IReadOnlyList<Finding> All(string source)
        => Compilation.Of(new SourceText(source, "Player.ron")).Findings;

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

    private const string Counter = "function index of (x => Number) { return x; }\n";
    private const string Reaching = "function index of bank (x => Number) { return x; }\n";

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
        // authors are innocent of a name neither of them spelled — and here
        // nobody could have avoided it either, because the pattern's words end
        // inside the «index of» the compiler adds, so every loop in every
        // program collides.
        var finding = Assert.IsType<NameShadowsPattern>(Only(
            Counter + "var banks => Number;\nfor each bank in banks { return index of bank; }\n"));

        Assert.True(finding.Universal);
        Assert.Equal("index of (_)", finding.Pattern);

        // The SHAPE rather than «index of bank», because no loop variable is at
        // fault and naming one implies a rename would help. It is also what
        // makes two loops one finding rather than two — see below.
        Assert.Equal("index of «a loop variable»", finding.Name);
        Assert.Contains("no name in the source avoids this", finding.Message);

        // The CARET on the pattern, which is line 1 here — earlier than the loop
        // on line 3, and blamed anyway. Asserting the message alone leaves the
        // span free to point at the loop while the sentence asks for the
        // pattern, which is the mismatch a previous round found on another rule.
        Assert.StartsWith("Player.ron:1:10:", Diagnostics.Report(finding));

        // And NOTHING alongside. A related span would have to pick one loop out
        // of however many are in scope, which is the same false implication as
        // naming one counter.
        Assert.Empty(finding.Related);
    }

    [Fact(DisplayName = "and it is reported once however many loops there are")]
    public void AndItIsReportedOnceHoweverManyLoopsThereAre()
    {
        // Found by audit. One invalid relationship, one repair, and one finding
        // per loop in scope — the messages differed only in the counter they
        // interpolated, which was enough to slip past the deduplication that
        // exists for exactly this. With N loops it was N complaints about the
        // same pattern, each asking for the same edit.
        //
        // The dedup is not a special case here: the finding stopped naming a
        // particular counter, so the two are the same finding and the ordinary
        // rule that a finding is recorded once does the rest.
        var finding = Assert.IsType<NameShadowsPattern>(Only(
            Counter
          + "var banks => Number;\nvar branches => Number;\n"
          + "for each bank in banks { return index of bank; }\n"
          + "for each branch in branches { return index of branch; }\n"));

        Assert.StartsWith("Player.ron:1:10:", Diagnostics.Report(finding));
    }

    [Theory(DisplayName = "and a pattern reaching into the subject blames the later declaration")]
    [InlineData(true)]
    [InlineData(false)]
    public void AndAPatternReachingIntoTheSubjectBlamesTheLaterDeclaration(bool patternFirst)
    {
        // Found by audit. Every generated collision was treated as though only
        // the pattern could change, which is true of «index of (_)» and false
        // of «index of bank (_)»: that one collides only with counters for
        // variables beginning «bank», so renaming the variable works as well as
        // respelling the pattern. Blaming the pattern regardless pointed at an
        // earlier declaration that was correct when it was written, and asked
        // for a larger change than the one that fixes it.
        var finding = Assert.IsType<NameShadowsPattern>(Only(
            patternFirst
          ? Reaching
          + "var accounts => Number;\n"
          + "for each (bank account) in accounts { return index of bank account; }\n"
          : "var accounts => Number;\n"
          + "for each (bank account) in accounts {\n"
          + "    " + Reaching
          + "    return index of bank account;\n}\n"));

        Assert.False(finding.Universal);
        Assert.Equal("index of bank account", finding.Name);
        Assert.Equal("bank account", finding.InjectedBy);
        Assert.Contains("rename that, or respell the pattern", finding.Message);

        // Line 3 either way, which is the later declaration either way: the loop
        // when the pattern came first, the pattern when it did not.
        //
        // The pattern goes INSIDE the loop for the second order, which is how
        // the case was reported and is not incidental. A pattern written after
        // the loop at file scope is still blamed on the loop, because ordering
        // across scopes is provenance rather than offset — an enclosing
        // declaration is taken to precede anything nested in it. That is a
        // property of the convention and not of this rule: a written «print job»
        // in a loop behaves the same way against a «print (_)» declared below.
        Assert.StartsWith("Player.ron:3:", Diagnostics.Report(finding));
    }

    [Theory(DisplayName = "and either repair it offers actually removes it")]
    [InlineData("branch account", "bank")]
    [InlineData("bank account", "branch")]
    public void AndEitherRepairItOffersActuallyRemovesIt(string variable, string reaches)
        // The message names two edits, so both have to work. Asserting the
        // wording alone would let it prescribe a rename that changes nothing —
        // which is the defect it was written to fix, one layer along.
        => Assert.Empty(All($"function index of {reaches} (x => Number) {{ return x; }}\n"
                          + "var accounts => Number;\n"
                          + $"for each ({variable}) in accounts {{ return index of {variable}; }}\n"));

    [Fact(DisplayName = "and a subject already blamed does not offend a second time")]
    public void AndASubjectAlreadyBlamedDoesNotOffendASecondTime()
    {
        // «print job» shadows «print (_)» on its own account, and the counter
        // built from it shadows «index of print (_)» for the same words. One
        // mistake, one rename that answers both — so one finding, against the
        // name somebody actually wrote.
        var finding = Assert.IsType<NameShadowsPattern>(Only(
            Pattern
          + "function index of print (x => Number) { return x; }\n"
          + "var banks => Number;\n"
          + "for each (print job) in banks { return print job; }\n"));

        Assert.Null(finding.InjectedBy);
        Assert.Equal("print job", finding.Name);
        Assert.StartsWith("Player.ron:4:11:", Diagnostics.Report(finding));
    }

    [Fact(DisplayName = "but not when the collision would outlive that repair")]
    public void ButNotWhenTheCollisionWouldOutliveThatRepair()
    {
        // The suppression is for a collision the subject CAUSED. A universal one
        // is not: rename «p is q» to anything at all and «index of (_)» still
        // collides with the counter, so hiding it behind the rename would send
        // the author back to a second error they were never shown.
        //
        // Both findings, and they are two mistakes rather than one.
        var source = "var p => Number;\nvar q => Number;\nvar banks => Number;\n"
                   + "for each (p is q) in banks { return index of p is q; }\n";

        Assert.Equal([FindingKind.InfixInName, FindingKind.NameShadowsPattern],
                     All(Counter + source).Select(finding => finding.Kind));

        // The evidence that they are, rather than the assertion that they are:
        // the repair the first one asks for leaves the second standing.
        Assert.Equal(FindingKind.NameShadowsPattern,
                     Only(Counter + source.Replace("p is q", "p and q")).Kind);
    }

    [Fact(DisplayName = "and an ordinary loop still generates nothing to complain about")]
    public void AndAnOrdinaryLoopStillGeneratesNothingToComplainAbout()
        // The rule is about a collision, so with no «index of» pattern in scope
        // the counter is an ordinary generated name and says nothing.
        => Assert.Empty(Compilation.Of(new SourceText(
            "var banks => Number;\nfor each bank in banks { return bank; }\n", "Player.ron")).Findings);
}
