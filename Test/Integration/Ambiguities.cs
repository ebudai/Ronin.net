// Copyright © 2026 Eric Budai

using Ronin.Compiler;

namespace Integration;

/// <summary>
///     Ambiguity as an error, from source rather than from a symbol table.
/// </summary>
///
/// <remarks>
///     <para>
///     The resolver could be reached only from its own tests, so an ambiguous
///     statement in a real file produced no finding and could not fail a build.
///     Every rule that refuses a name at its declaration exists to keep this
///     error answerable, and none of them was answering to anything — the
///     central promise of the direction was true of a class nobody called.
///     </para>
///     <para>
///     These go through <see cref="Compilation"/>, which is the whole point: a
///     hand-built table can be given names the declaration rules would refuse,
///     and has been.
///     </para>
/// </remarks>
[Trait(nameof(Compilation), null)]
public class Ambiguities
{
    private static IReadOnlyList<Finding> All(string source)
        => Compilation.Of(new SourceText(source, "Player.ron")).Findings;

    /// <remarks>
    ///     «a to b» is legal beside «send (_) to (_)» — its own span reads only
    ///     as itself, so the surviving rules admit it and the ambiguity it causes
    ///     lands here instead, where a bracket reaches it.
    /// </remarks>
    private const string Colliding =
        "function send (x => Number) { return x; }\n" +
        "function send (x => Number) to (y => Number) { return x; }\n" +
        "var a to b => Number;\n";

    [Fact(DisplayName = "an ambiguous statement in real source is a finding")]
    public void AnAmbiguousStatementInRealSourceIsAFinding()
    {
        var finding = Assert.IsType<Ambiguous>(Assert.Single(
            All(Colliding + "var a => Number;\nvar b => Number;\nvar result = send a to b;\n")));

        Assert.Equal(["send «a to b»", "send «a» to «b»"], finding.Readings);
        Assert.Equal(2, finding.Total);
        Assert.False(finding.Bounded);

        // The caret on the EXPRESSION and not on the statement's first word: the
        // reading is what has two meanings, and «var result =» has one.
        Assert.StartsWith("Player.ron:6:14:", Diagnostics.Report(finding));
    }

    [Fact(DisplayName = "and it is read against the scope it was written in")]
    public void AndItIsReadAgainstTheScopeItWasWrittenIn()
    {
        // «a» is a parameter and «b» is local, so neither exists in the enclosing
        // table — the statement resolves only against the body's own. Walking a
        // scope's statements without stopping where a body begins would have read
        // this one against the module and found no parse at all.
        var finding = Assert.IsType<Ambiguous>(Assert.Single(
            All(Colliding + "function go (a => Number) { var b => Number; var r = send a to b; }\n")));

        Assert.Equal(["send «a to b»", "send «a» to «b»"], finding.Readings);
    }

    [Theory(DisplayName = "and either bracketing answers it")]
    [InlineData("send (a to b)")]
    [InlineData("send (a) to (b)")]
    public void AndEitherBracketingAnswersIt(string repaired)
        // The message says to bracket, so bracketing has to work — a repair that
        // does not is worse than none, and this is the first place the claim is
        // made to real source rather than to a table built for it.
        => Assert.Empty(All(Colliding + $"var a => Number;\nvar b => Number;\nvar result = {repaired};\n"));

    [Fact(DisplayName = "and a statement with more readings than fit says how many")]
    public void AndAStatementWithMoreReadingsThanFitSaysHowMany()
    {
        // Three independently ambiguous operands of one expression, so eight
        // readings and room for five. A list that stops without saying so reads
        // as "these are all of them", and a reader choosing among five would be
        // choosing from a set nobody told them was partial.
        var finding = Assert.IsType<Ambiguous>(Assert.Single(
            All(Colliding + "var a => Number;\nvar b => Number;\n"
              + "var result = (send a to b) + (send a to b) + (send a to b);\n")));

        Assert.True(finding.Bounded);
        Assert.Equal(Resolver.Kept, finding.Readings.Count);
        Assert.Equal(8, finding.Total);
        Assert.Contains("at least 8", finding.Message);
    }

    [Fact(DisplayName = "and one mistake is one finding, however it is bracketed")]
    public void AndOneMistakeIsOneFindingHoweverItIsBracketed()
    {
        // A bracketed part is a reference of its own, so this held three — the
        // whole expression and each half — and said the same thing at three
        // spans. The whole expression's readings already contain every
        // combination of its parts', and they are the ones a reader brackets.
        var finding = Assert.IsType<Ambiguous>(Assert.Single(
            All(Colliding + "var a => Number;\nvar b => Number;\n"
              + "var result = (send a to b) + (send a to b);\n")));

        Assert.Equal(4, finding.Total);

        // SEPARATE statements stay separate, which is the other half: each
        // element of a list is the outermost expression of its own subtree, so
        // three ambiguous elements are three mistakes with three repairs.
        Assert.Equal(3, All(Colliding + "var a => Number;\nvar b => Number;\n"
                          + "var result = (send a to b, send a to b, send a to b);\n").Count);
    }

    [Fact(DisplayName = "and a body's statement is read once, by the scope that owns it")]
    public void AndABodysStatementIsReadOnceByTheScopeThatOwnsIt()
    {
        // Everything the body uses is also in scope outside it, so without the
        // walk stopping at the body the enclosing scope reads this statement
        // too.
        //
        // And this test does NOT prove that it stops: removing the stop leaves
        // it green, because the second reading is the same reading at the same
        // span and «Compilation.Add» records a finding once. What it pins is the
        // count a reader sees; the stop is there because a body's statements are
        // the body's to read, which is a statement about scope rather than about
        // how many messages come out. Worth saying rather than implying the
        // assertion is stronger than it is.
        Assert.Single(All(Colliding + "var a => Number;\nvar b => Number;\n"
                        + "function go { var r = send a to b; }\n"));
    }

    [Fact(DisplayName = "and a type annotation is not read as a value")]
    public void AndATypeAnnotationIsNotReadAsAValue()
    {
        // A type is a reference too — «=> list of number» is a run of words
        // awaiting a meaning, exactly as a statement is — so the walk read every
        // annotation in the file against the VALUE table, where they mean
        // nothing. Mostly that produced a no-reading nobody reports. Here it
        // reported an ambiguity about a TYPE, quoting two readings that were
        // never in question, at a position where neither could be written.
        //
        // Types resolve against a table that does not exist yet, and reading
        // them against the wrong one is worse than not reading them at all.
        Assert.Empty(All(Colliding + "var a => Number;\nvar b => Number;\nvar thing => send a to b;\n"));
    }

    [Fact(DisplayName = "«return» is a pattern, so a name may not capture it")]
    public void ReturnIsAPatternSoANameMayNotCaptureIt()
    {
        // A word that parses must live in the table the name rules run over,
        // because a keyword is a name those rules cannot see. As a keyword,
        // «return value» stays declarable — and then WINS, at one lookup against
        // the call's two, silently. As a pattern the rule that refuses every
        // other capture refuses it.
        var finding = Assert.IsType<NameShadowsPattern>(Assert.Single(All("var return value => Number;\n")));

        Assert.Equal("return (_)", finding.Pattern);
        Assert.True(finding.Builtin);

        // EQUAL is NOT left alone, and this asserted that it was. The rule that
        // leaves a name equal to a pattern's words alone reasons that the call's
        // argument would have to sit beside it as a second juxtaposed name — but
        // bare «return» has no argument, so nothing has to sit anywhere and the
        // name covers exactly the span the call does.
        //
        // Which is a capture: with «var return» declared, every bare «return» in
        // scope reads two ways, and no bracket separates a name from a call over
        // one span. So the whole spelling is reserved, by the same door the two
        // truths use.
        var whole = Assert.Single(All("var return => Number;\n"));

        Assert.Equal(FindingKind.Supplied, whole.Kind);
        Assert.Contains("«return» is supplied by the language", whole.Message);
    }

    [Fact(DisplayName = "and a body's return is read, where it used to be nothing at all")]
    public void AndABodysReturnIsReadWhereItUsedToBeNothingAtAll()
    {
        // It was in every fixture in this suite and resolved in none of them:
        // «return» was not a keyword, not a pattern, not in any table, so a
        // function body's last statement was a run of words containing one
        // nothing could look up. The runtime has had a «Return» the whole time.
        var compilation = Compilation.Of(new SourceText(
            "function twice (x => Number) { return x; }\nvar n => Number;\nvar r = twice n;\n", "Player.ron"));

        Assert.Empty(compilation.Findings);

        Assert.Equal(["return «x»", "twice «n»"],
                     compilation.Readings.Select(reading => reading.Resolution.Reading).Order());
    }

    [Fact(DisplayName = "«optional» is a type constructor, so a name may not capture it either")]
    public void OptionalIsATypeConstructorSoANameMayNotCaptureItEither()
    {
        // It was a MODIFIER keyword — a word that parses and is not in the table
        // the name rules run over — so «optional value» was declarable and
        // captured. It was also the last type constructor that was not a
        // pattern, every other one already being one, so leaving it a keyword
        // was the fork rather than the change.
        var finding = Assert.IsType<NameShadowsPattern>(Assert.Single(All("var optional value => Number;\n")));

        Assert.Equal("optional (_)", finding.Pattern);
        Assert.True(finding.Builtin);

        Assert.Empty(All("var optional => Number;\n"));
    }

    [Theory(DisplayName = "a body leaves one way or the other, and a reaction never answers")]
    [InlineData("function twice (x => Number) { return x; }", null)]
    [InlineData("function shout (x => Number) { return; }", null)]
    [InlineData("var ready => Number;\nwhen ready { return; }", null)]
    [InlineData("var ready => Number;\nwhen ready { return 1; }", "AnsweringReaction")]
    [InlineData("function odd (x => Number) { return; return x; }", "MixedExits")]
    [InlineData("let reading = 1;\nfunction smooth { return old reading; }", null)]
    public void ABodyLeavesOneWayOrTheOtherAndAReactionNeverAnswers(string source, string refused)
    {
        // «return (_)» and bare «return» are one concept at two arities — leave
        // this body now, with or without an answer — so a body has ONE exit
        // flavour, decided by whether any «return (_)» appears in it. That is
        // not a rule of its own: it is the check that stops the return type
        // having two answers, seen from the other side.
        //
        // A reaction has nobody to answer, so only the valueless form is legal
        // in a «when». Its message names the two neighbouring words rather than
        // leaving a newcomer to work out which of «return» and «stop» they
        // wanted.
        var findings = All(source + "\n");

        if (refused is null)
        {
            Assert.Empty(findings);
            return;
        }

        Assert.Equal(refused, Assert.Single(findings).Kind.ToString());
    }

    [Fact(DisplayName = "and an exit is found wherever it sits, not only at the top")]
    public void AndAnExitIsFoundWhereverItSitsNotOnlyAtTheTop()
    {
        // «return» is a call like any other, so it can sit inside one. Looking
        // only at the top of a statement would answer for the shapes people
        // write and stay silent on the ones they do not, which is the wrong way
        // round for a rule about legality.
        //
        // TWO, because there are two of them. This asserted one, and got one
        // because every match reported the whole statement's span and two
        // findings sharing a kind, a span and a message are recorded once. Two
        // edits arriving as one message, and the assertion agreed with it.
        var nested = All("var ready => Number;\nwhen ready { return (return 1); }\n");

        Assert.Equal(2, nested.Count);
        Assert.All(nested, finding => Assert.Equal(FindingKind.AnsweringReaction, finding.Kind));
    }

    [Fact(DisplayName = "and two exits in one statement are two edits at two places")]
    public void AndTwoExitsInOneStatementAreTwoEditsAtTwoPlaces()
    {
        // Found by audit. Neither «return» is at the top of the statement and
        // neither contains the other, so nothing about the shape hides one — the
        // span did, because a resolved call knew what it meant and not where it
        // was.
        var findings = All("function send (x => Number) to (y => Number) { return x; }\n"
                         + "var ready => Number;\n"
                         + "when ready { send (return 1) to (return 2); }\n");

        Assert.Equal(["Player.ron:3:20:", "Player.ron:3:34:"],
                     findings.Select(finding => Diagnostics.Report(finding)[..16]).Order());
    }

    [Fact(DisplayName = "the two truths are supplied, not declared")]
    public void TheTwoTruthsAreSuppliedNotDeclared()
    {
        // A literal is a NULLARY entry — a name, not a pattern — so each reserves
        // its own spelling and nothing else. «true positive» and «truth table»
        // stay legal, which they would not if these were anchor-only patterns.
        var compilation = Compilation.Of(new SourceText("var ok = true;\nvar off = false;\n", "Player.ron"));

        Assert.Empty(compilation.Findings);
        Assert.Equal(["«false»", "«true»"], compilation.Readings.Select(r => r.Resolution.Reading).Order());

        Assert.Empty(Compilation.Of(new SourceText("var true positive => Number;\n", "Player.ron")).Findings);

        // SUPPLIED rather than declared, so «already declared here, rename this
        // one» would point at a declaration that does not exist. The pattern
        // case has said this properly since «old (_)» arrived, and this is the
        // same sentence about the same thing.
        var refused = Assert.Single(Compilation.Of(new SourceText("var true => Number;\n", "Player.ron")).Findings);

        Assert.Equal(FindingKind.Supplied, refused.Kind);
        Assert.Contains("«true» is supplied by the language", refused.Message);
    }

    [Fact(DisplayName = "the error carries the edits that answer it")]
    public void TheErrorCarriesTheEditsThatAnswerIt()
    {
        // A message cannot be clicked. The bracketings are IN the error and are
        // edits with positions, because an editor applies those and can only
        // print a sentence describing where a bracket would go.
        const string Source = "function send (x => Number) { return x; }\n"
                            + "function send (x => Number) to (y => Number) { return x; }\n"
                            + "var a to b => Number;\nvar a => Number;\nvar b => Number;\n"
                            + "var result = send a to b;\n";

        var finding = Assert.IsType<Ambiguous>(Assert.Single(All(Source)));

        // CHEAPEST FIRST, which is the whole of what cost does now: it may order
        // the suggestions and may never choose among them.
        Assert.Equal([0, 1], finding.Repairs.Select(repair => repair.Rank));
        Assert.Equal(["send «a to b»", "send «a» to «b»"], finding.Repairs.Select(repair => repair.Reading));

        // And APPLYING each one leaves a file that compiles, reading the way the
        // repair said it would. Asserting the offsets alone would let this offer
        // an edit that puts a bracket somewhere plausible and wrong — which is
        // the defect a suggestion has when nobody types it.
        foreach (var repair in finding.Repairs)
        {
            var edited = Source;

            foreach (var insertion in repair.Insertions.OrderByDescending(insertion => insertion.At))
            {
                edited = edited[..insertion.At] + insertion.Text + edited[insertion.At..];
            }

            var repaired = Compilation.Of(new SourceText(edited, "Player.ron"));

            Assert.Empty(repaired.Findings);
            Assert.Contains(repair.Reading.Replace("«", string.Empty, StringComparison.Ordinal)
                                          .Replace("»", string.Empty, StringComparison.Ordinal)
                                          .Replace(" ", string.Empty, StringComparison.Ordinal),
                            Assert.Single(repaired.Readings, reading => reading.Span.Offset > 100)
                                  .Resolution.Reading
                                  .Replace("«", string.Empty, StringComparison.Ordinal)
                                  .Replace("»", string.Empty, StringComparison.Ordinal)
                                  .Replace("⟨", string.Empty, StringComparison.Ordinal)
                                  .Replace("⟩", string.Empty, StringComparison.Ordinal)
                                  .Replace(" ", string.Empty, StringComparison.Ordinal));
        }
    }

    [Theory(DisplayName = "«stop» is reserved everywhere and legal only in a «when»")]
    [InlineData("var ready => Number;\nwhen ready { stop; }", null)]
    [InlineData("function go { stop; }", "MisplacedStop")]
    [InlineData("var stop => Number;", "Supplied")]
    [InlineData("var stop word => Number;", null)]
    public void StopIsReservedEverywhereAndLegalOnlyInAWhen(string source, string refused)
    {
        // RESERVED globally and LEGAL only in a «when», which are separate on
        // purpose. Scoping the reservation to a «when» is tempting and wrong for
        // a reason already paid for: the self-ambiguity check is deliberately
        // pessimistic so that it is order-independent, and a reservation that
        // depends on where you are standing gives that check two answers for one
        // span — and lets a name declared outside a «when» be captured inside
        // one.
        //
        // Whole-name only, so «stop word» stays legal: a nullary entry reserves
        // its own spelling and nothing else.
        var findings = All(source + "\n");

        if (refused is null)
        {
            Assert.Empty(findings);
            return;
        }

        Assert.Equal(refused, Assert.Single(findings).Kind.ToString());
    }

    [Fact(DisplayName = "two readings that print alike get the two different repairs")]
    public void TwoReadingsThatPrintAlikeGetTheTwoDifferentRepairs()
    {
        // Found by audit. The resolver was taught to keep alternatives apart by
        // shape rather than by how they read, and then handed them over as
        // renderings — so the repair layer inherited exactly the non-injectivity
        // that had been removed one level down. Both searches looked for the
        // same sentence, both found the same bracket, and one of the two
        // meanings could not be selected from the diagnostic at all.
        const string Source = "function send (x => Number) { return x; }\n"
                            + "function send (x => Number) to (y => Number) { return x; }\n"
                            + "function print (x => Number) { return x; }\n"
                            + "function print (x => Number) to (y => Number) { return x; }\n"
                            + "var a => Number;\nvar b => Number;\nvar result = print send a to b;\n";

        var finding = Assert.IsType<Ambiguous>(Assert.Single(All(Source)));

        // The same sentence twice, which is the whole difficulty: nothing about
        // the readings tells them apart, and they are still two meanings.
        Assert.Equal(["print send «a» to «b»", "print send «a» to «b»"], finding.Readings);

        // TWO different edits, and each produces a file that compiles.
        var repaired = finding.Repairs.Select(repair => Applied(Source, repair)).ToArray();

        Assert.Equal(2, repaired.Distinct(StringComparer.Ordinal).Count());
        Assert.All(repaired, edited => Assert.Empty(All(edited)));

        Assert.Contains(repaired, edited => edited.Contains("print (send a to b)", StringComparison.Ordinal));
        Assert.Contains(repaired, edited => edited.Contains("print (send a) to b", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "and a reading that needs two brackets gets two brackets")]
    public void AndAReadingThatNeedsTwoBracketsGetsTwoBrackets()
    {
        // Found by audit. Each of these four readings chooses a meaning for the
        // LEFT child and one for the RIGHT, so selecting one means disambiguating
        // both — two bracket pairs, where the search used to look for exactly one
        // and publish an empty repair when none was found.
        //
        // I claimed one pair always suffices, citing the repair-completeness
        // property — which generates flat word sequences and never composes
        // ambiguous children, so it never reaches this shape. The search tries
        // the tree's own spans now and, where a single fails, pairs of them.
        const string Source = "function send (x => Number) { return x; }\n"
                            + "function send (x => Number) to (y => Number) { return x; }\n"
                            + "var a to b => Number;\nvar a => Number;\nvar b => Number;\n"
                            + "var result = (send a to b) + (send a to b);\n";

        var finding = Assert.IsType<Ambiguous>(Assert.Single(All(Source)));

        Assert.Equal(4, finding.Readings.Count);
        Assert.Equal(4, finding.Repairs.Count);

        // Two pairs each, and applying every one leaves a file that compiles.
        Assert.All(finding.Repairs, repair =>
        {
            Assert.Equal(4, repair.Insertions.Count);
            Assert.Empty(All(Applied(Source, repair)));
        });
    }

    [Fact(DisplayName = "and a reading that needs three brackets gets three brackets")]
    public void AndAReadingThatNeedsThreeBracketsGetsThreeBrackets()
    {
        // Found by audit. Three independently ambiguous children, so a complete
        // reading fixes a meaning for all three at once and needs a bracket
        // around each — three pairs. The search had generalised from one pair to
        // exactly two, which is the same fixed-arity assumption moved up by one:
        // every reading here was reported with no selectable repair, its own
        // count-and-cap test never asking whether one existed. Sets of the tree's
        // spans are tried by increasing size now, so a reading pinning three
        // children is reached at size three.
        const string Source = "function send (x => Number) { return x; }\n"
                            + "function send (x => Number) to (y => Number) { return x; }\n"
                            + "var a to b => Number;\nvar a => Number;\nvar b => Number;\n"
                            + "var result = (send a to b) + (send a to b) + (send a to b);\n";

        var finding = Assert.IsType<Ambiguous>(Assert.Single(All(Source)));

        // Eight readings, five shown, and a repair for every shown one — where
        // the two-level search published none at all.
        Assert.Equal(8, finding.Total);
        Assert.Equal(Resolver.Kept, finding.Repairs.Count);

        // Three pairs each, and five different edits: the search finds a distinct
        // bracketing for each reading rather than the same one repeatedly.
        Assert.All(finding.Repairs, repair => Assert.Equal(6, repair.Insertions.Count));
        Assert.Equal(Resolver.Kept,
                     finding.Repairs.Select(repair => Applied(Source, repair)).Distinct(StringComparer.Ordinal).Count());

        // And applying each one leaves a file that compiles AND reads the way the
        // repair named it — structurally, so that «send (a to b)» and «send (a)
        // to b» are told apart. Stripping only the group marks keeps the name
        // marks that distinguish the two, which a full strip would collapse; that
        // collapse is the very non-injectivity the tree-based search removed.
        foreach (var repair in finding.Repairs)
        {
            var edited = Applied(Source, repair);

            Assert.Empty(All(edited));
            Assert.Equal(Grouped(repair.Reading), Grouped(Selected(edited)));
        }
    }

    [Fact(DisplayName = "and eight ambiguous children are repaired without the exponential cost")]
    public void AndEightAmbiguousChildrenAreRepairedWithoutTheExponentialCost()
    {
        // Found by audit. Eight independently ambiguous children — 256 readings,
        // five shown — each needing a bracket around every child. Enumerating the
        // spans' subsets reached that eight-bracket set only past every smaller
        // set that fails first, which is O(2ⁿ): nine seconds, eleven gigabytes,
        // and not one repair offered for any shown reading. Bracketing the whole
        // tree and trimming is O(nodes), so the repairs are here.
        var source = Colliding + "var a => Number;\nvar b => Number;\nvar result = "
                   + string.Join(" + ", Enumerable.Repeat("(send a to b)", 8)) + ";\n";

        var finding = Assert.IsType<Ambiguous>(Assert.Single(All(source)));

        Assert.Equal(256, finding.Total);

        // A repair for every shown reading, where the subset search offered none,
        // and each one applies to a file that compiles.
        Assert.Equal(Resolver.Kept, finding.Repairs.Count);
        Assert.All(finding.Repairs, repair => Assert.Empty(All(Applied(source, repair))));
    }

    [Fact(DisplayName = "and a reading containing a list is repaired around the list, not inside it")]
    public void AndAReadingContainingAListIsRepairedAroundTheListNotInsideIt()
    {
        // Found by audit. Bracketing every subtree put a group around the list's
        // element «a» in «[a]» that «Same» — which treats a collection as opaque —
        // then left in place, so the full candidate never matched the target and a
        // reading containing a list got no repair at all. The walk obeys the same
        // contract as the strip now: a collection is opaque, repaired around and
        // never inside.
        const string Source = "function send (x => Number) { return x; }\n"
                            + "function send (x => Number) to (y => Number) { return x; }\n"
                            + "function print (x => Number) { return x; }\n"
                            + "function print (x => Number) to (y => Number) { return x; }\n"
                            + "var a => Number;\nvar b => Number;\nvar result = print send [a] to b;\n";

        var finding = Assert.IsType<Ambiguous>(Assert.Single(All(Source)));

        Assert.Equal(2, finding.Total);
        Assert.Equal(2, finding.Repairs.Count);

        // two different edits, each to a file that compiles
        var edited = finding.Repairs.Select(repair => Applied(Source, repair)).ToArray();

        Assert.Equal(2, edited.Distinct(StringComparer.Ordinal).Count());
        Assert.All(edited, source => Assert.Empty(All(source)));

        // around the list — the bracket never falls inside «[a]»
        Assert.Contains(edited, source => source.Contains("print (send [a] to b)", StringComparison.Ordinal));
        Assert.Contains(edited, source => source.Contains("print (send [a]) to b", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "and a reading's one wide bracket is not buried behind a large unambiguous argument")]
    public void AndAReadingsOneWideBracketIsNotBuriedBehindALargeUnambiguousArgument()
    {
        // Found by audit. «print send (a + a + … forty-two times) to b» has two
        // readings, each fixed by one bracket around a WIDE call. Growing the
        // bracketing narrowest first appended every name and operation node of the
        // unambiguous sum before reaching that call — a candidate that outgrew its
        // own one-pair answer and, at eighty-nine lexemes, crossed the resolver's
        // ceiling before it could select, so the statement got no repair at all.
        // A bracket a competitor lacks is tried before the idle ones now.
        var sum = string.Join(" + ", Enumerable.Repeat("a", 42));
        var source = "function send (x => Number) { return x; }\n"
                   + "function send (x => Number) to (y => Number) { return x; }\n"
                   + "function print (x => Number) { return x; }\n"
                   + "function print (x => Number) to (y => Number) { return x; }\n"
                   + "var a => Number;\nvar b => Number;\nvar result = print send (" + sum + ") to b;\n";

        var finding = Assert.IsType<Ambiguous>(Assert.Single(All(source)));

        Assert.Equal(2, finding.Total);
        Assert.Equal(2, finding.Repairs.Count);

        // one pair each, two different edits, and each to a file that compiles
        Assert.All(finding.Repairs, repair => Assert.Equal(2, repair.Insertions.Count));

        var edited = finding.Repairs.Select(repair => Applied(source, repair)).ToArray();

        Assert.Equal(2, edited.Distinct(StringComparer.Ordinal).Count());
        Assert.All(edited, source => Assert.Empty(All(source)));
    }

    /// <summary>The reading the repaired file resolves its result statement to.</summary>
    private static string Selected(string edited)
        => Compilation.Of(new SourceText(edited, "Player.ron"))
                      .Readings
                      .Where(reading => reading.Resolution.Kind is ResolutionKind.Resolved && reading.Span.Offset > 120)
                      .OrderByDescending(reading => reading.Span.Length)
                      .First()
                      .Resolution.Reading;

    /// <summary>A reading with the grouping marks removed but the name marks kept.</summary>
    ///
    /// <remarks>
    ///     A repair adds brackets, so the file it produces reads with more groups
    ///     than the bare tree the repair names — the group marks differ and the
    ///     structure does not. The name marks stay, because they are what tells
    ///     «send «a to b»» from «send «a» to «b»», and a check that dropped them
    ///     would pass for a repair that selected the wrong one of the two.
    /// </remarks>
    private static string Grouped(string reading)
        => reading.Replace("⟨", string.Empty, StringComparison.Ordinal)
                  .Replace("⟩", string.Empty, StringComparison.Ordinal);

    /// <summary>The source with one repair's brackets typed into it.</summary>
    private static string Applied(string source, Repair repair)
    {
        var edited = source;

        foreach (var insertion in repair.Insertions.OrderByDescending(insertion => insertion.At))
        {
            edited = edited[..insertion.At] + insertion.Text + edited[insertion.At..];
        }

        return edited;
    }

    [Fact(DisplayName = "and an ambiguity inside a list is bracketed inside the list")]
    public void AndAnAmbiguityInsideAListIsBracketedInsideTheList()
    {
        // A collection's element is its own reference, so the ambiguity in
        // «[send a to b]» is reported on the element and repaired there — a
        // bracket around «a to b», inside the list. This is the list standing in
        // for the ordinary case, to show the finding and its repairs arrive
        // through a collection as they do anywhere.
        const string Source = "function send (x => Number) { return x; }\n"
                            + "function send (x => Number) to (y => Number) { return x; }\n"
                            + "var a to b => Number;\nvar a => Number;\nvar b => Number;\n"
                            + "var result = [send a to b];\n";

        var finding = Assert.IsType<Ambiguous>(Assert.Single(All(Source)));

        Assert.Equal(2, finding.Repairs.Count);
        Assert.All(finding.Repairs, repair => Assert.Empty(All(Applied(Source, repair))));
    }

    [Fact(DisplayName = "and an unambiguous file says nothing")]
    public void AndAnUnambiguousFileSaysNothing()
        // The same statement with the colliding name gone. Without it there is
        // one reading, and a rule that fired anyway would be refusing the
        // language rather than an ambiguity in it.
        => Assert.Empty(All("function send (x => Number) { return x; }\n"
                          + "function send (x => Number) to (y => Number) { return x; }\n"
                          + "var a => Number;\nvar b => Number;\nvar result = send a to b;\n"));
}
