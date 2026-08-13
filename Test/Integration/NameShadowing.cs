// Copyright © 2026 Eric Budai

using Ronin.Compiler;

namespace Integration;

/// <summary>
///     A name whose own complete span also reads as a pattern call.
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

    /// <summary>
    ///     The one declaration finding, past the use sites it causes.
    /// </summary>
    ///
    /// <remarks>
    ///     A shape these fixtures could not have before «return (_)» became a
    ///     callable pattern: «return index of bank» now reads two ways, as the
    ///     counter or as a call, so the very collision the declaration rule
    ///     refuses is ALSO reported where it is written. Both are true and both
    ///     go together when the pattern is respelled — this asks about the
    ///     declaration half, and the test below asks that the other half is
    ///     there rather than letting it hide behind a filter.
    /// </remarks>
    private static NameShadowsPattern Declared(string source)
        => Assert.Single(All(source).OfType<NameShadowsPattern>());

    private static IReadOnlyList<Finding> All(string source)
        => Compilation.Of(new SourceText(source, "Player.ron")).Findings;

    // AN ACTION, and the return is what decides which side of the shrink this
    // sits on rather than a detail of the fixture. «print» is conceptually one,
    // and «print job» is the example the design gives of a name the type
    // checker takes back: «nothing» differs from every value type, so the call
    // reading is eliminated in a value position and the name in a statement
    // one. Written as returning a Number it would have been the opposite case
    // while claiming to be this one.
    private const string Pattern = "function print (x => number) { }\n";
    private const string Name = "var print job => number;\n";

    [Trait(Expiry.Shrink, Expiry.Expires)]
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

    // EXPIRES in both rows, by the declaration text alone: send returns nothing
    // against a Number name; sort returns a List against a Text name. The
    // criterion is the pattern's return type rather than action-versus-value.
    [Trait(Expiry.Shrink, Expiry.Expires)]
    [Theory(DisplayName = "and glued own-span calls are refused until types eliminate them")]
    [InlineData("var send x to y => number;\n"
              + "function send (x => number) to (y => number) { }\n",
                "send x to y", "send (_) to (_)")]
    [InlineData("var sort order => text;\n"
              + "function sort (items => list) => list { return items; }\n",
                "sort order", "sort (_)")]
    public void AndGluedOwnSpanCallsAreRefusedUntilTypesEliminateThem(string source, string name, string pattern)
    {
        var finding = Assert.IsType<NameShadowsPattern>(Only(source));

        Assert.Equal(name, finding.Name);
        Assert.Equal(pattern, finding.Pattern);
    }

    [Trait(Expiry.Shrink, Expiry.Survives)]
    [Fact(DisplayName = "and a same-type own-span call survives the shrink")]
    public void AndASameTypeOwnSpanCallSurvivesTheShrink()
    {
        var finding = Assert.IsType<NameShadowsPattern>(Only(
            "var sum of items => number;\n"
          + "function sum of (items => number) => number { return items; }\n"));

        Assert.Equal("sum of items", finding.Name);
        Assert.Equal("sum of (_)", finding.Pattern);
    }

    [Fact(DisplayName = "but glue inside a name is legal when its own span is not a call")]
    public void ButGlueInsideANameIsLegalWhenItsOwnSpanIsNotACall()
        => Assert.Empty(All("var a to b => number;\n"
                          + "function send (x => number) to (y => number) { }\n"));

    [Fact(DisplayName = "and pinned holes consume exactly one name word")]
    public void AndPinnedHolesConsumeExactlyOneNameWord()
    {
        SourceText source = new(string.Empty);
        var span = source.Span(0, 0);
        Shape pattern = new(new Ronin.Compiler.Pattern(["take", null, "in", null], [1]), span);

        Assert.Single(Rules.Validate([new Declared("take one in things", span)], [pattern]));
        Assert.Empty(Rules.Validate([new Declared("take one two in things", span)], [pattern]));

        // Nothing remains for the pinned hole. This is distinct from a free
        // trailing hole, which could consume several words but still needs one.
        Shape trailing = new(new Ronin.Compiler.Pattern(["take", null], [1]), span);
        Assert.Empty(Rules.Validate([new Declared("take", span)], [trailing]));
    }

    [Fact(DisplayName = "and overlapping hole partitions are memoized")]
    public void AndOverlappingHolePartitionsAreMemoized()
    {
        // Adjacent free holes reach the same suffix through several partitions.
        // The failed literal keeps every route live long enough to exercise the
        // memoized state rather than returning on the first successful split.
        SourceText source = new(string.Empty);
        var span = source.Span(0, 0);

        Assert.Empty(Rules.Validate([new Declared("send a b c", span)],
                                    [new Shape(Ronin.Compiler.Pattern.Parse("send _ _ end"), span)]));
    }

    [Fact(DisplayName = "and a name equal to the pattern's words is left alone")]
    public void AndANameEqualToThePatternsWordsIsLeftAlone()
    {
        // PROPER prefix. «print» cannot swallow the call, because the argument
        // would then have to sit beside it as a second juxtaposed name and that
        // is not an expression. Refusing it would be refusing something that
        // cannot go wrong.
        Assert.Empty(Compilation.Of(new SourceText("var print => number;\n" + Pattern, "Player.ron")).Findings);
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
            Only("function x otherwise (value => number) { return value; }\n"));

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
                        Only("function otherwise (value => number) { return value; }\n").Kind);

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

    private const string Counter = "function index of (x => number) { return x; }\n";
    private const string Reaching = "function index of bank (x => number) { return x; }\n";

    // SURVIVES: a loop counter is a number and «index of (_)» returns one, so
    // both readings are numbers in the same position and nothing eliminates
    // either. The same is true of every «index of» fixture below.
    [Trait(Expiry.Shrink, Expiry.Survives)]
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
        const string Looping = "var banks => number;\nfor each bank in banks { return index of bank; }\n";

        var finding = Declared(Counter + Looping);

        // And the use site says so too, because it genuinely does read two ways
        // once «return» is a call: the counter, or a return of the call. That is
        // the capture this rule refuses, arriving where it would have been felt.
        Assert.Equal([FindingKind.NameShadowsPattern, FindingKind.Ambiguous],
                     All(Counter + Looping).Select(each => each.Kind));

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

    [Trait(Expiry.Shrink, Expiry.Survives)]
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
        var finding = Declared(
            Counter
          + "var banks => number;\nvar branches => number;\n"
          + "for each bank in banks { return index of bank; }\n"
          + "for each branch in branches { return index of branch; }\n");

        Assert.StartsWith("Player.ron:1:10:", Diagnostics.Report(finding));
    }

    [Trait(Expiry.Shrink, Expiry.Survives)]
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
          + "var accounts => number;\n"
          + "for each (bank account) in accounts { return index of bank account; }\n"
          : "var accounts => number;\n"
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
        => Assert.Empty(All($"function index of {reaches} (x => number) {{ return x; }}\n"
                          + "var accounts => number;\n"
                          + $"for each ({variable}) in accounts {{ return index of {variable}; }}\n"));

    [Trait(Expiry.Shrink, Expiry.Survives)]
    [Fact(DisplayName = "and a subject already blamed does not offend a second time")]
    public void AndASubjectAlreadyBlamedDoesNotOffendASecondTime()
    {
        // «print job» shadows «print (_)» on its own account, and the counter
        // built from it shadows «index of print (_)» for the same words. One
        // mistake, one rename that answers both — so one finding, against the
        // name somebody actually wrote.
        var finding = Assert.IsType<NameShadowsPattern>(Only(
            Pattern
          + "function index of print (x => number) { return x; }\n"
          + "var banks => number;\n"
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
        var source = "var p => number;\nvar q => number;\nvar banks => number;\n"
                   + "for each (p is q) in banks { return index of p is q; }\n";

        // The third is the use site, which now reads two ways for the same
        // reason the second refuses the declaration — «return index of p is q»
        // is the counter or a return of the call, and both are real.
        Assert.Equal([FindingKind.InfixInName, FindingKind.NameShadowsPattern, FindingKind.Ambiguous],
                     All(Counter + source).Select(each => each.Kind).Distinct());

        // The evidence that they are, rather than the assertion that they are:
        // the repair the first one asks for leaves the second standing.
        Assert.Contains(FindingKind.NameShadowsPattern,
                        All(Counter + source.Replace("p is q", "p and q")).Select(each => each.Kind));
    }

    [Fact(DisplayName = "and an ordinary loop still generates nothing to complain about")]
    public void AndAnOrdinaryLoopStillGeneratesNothingToComplainAbout()
        // The rule is about a collision, so with no «index of» pattern in scope
        // the counter is an ordinary generated name and says nothing.
        => Assert.Empty(Compilation.Of(new SourceText(
            "var banks => number;\nfor each bank in banks { return bank; }\n", "Player.ron")).Findings);
}
