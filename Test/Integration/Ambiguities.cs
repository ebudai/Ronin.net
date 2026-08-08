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

        // EQUAL is left alone, as with every other pattern: «return» cannot
        // swallow the call, because the argument would have to sit beside it as
        // a second juxtaposed name and that is not an expression.
        Assert.Empty(All("var return => Number;\n"));
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
        // «return» is a call like any other, so it can sit inside one. Looking
        // only at the top of a statement would answer for the shapes people
        // write and stay silent on the ones they do not, which is the wrong way
        // round for a rule about legality.
        => Assert.Equal("AnsweringReaction",
                        Assert.Single(All("var ready => Number;\nwhen ready { return (return 1); }\n")).Kind.ToString());

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

    [Fact(DisplayName = "and an unambiguous file says nothing")]
    public void AndAnUnambiguousFileSaysNothing()
        // The same statement with the colliding name gone. Without it there is
        // one reading, and a rule that fired anyway would be refusing the
        // language rather than an ambiguity in it.
        => Assert.Empty(All("function send (x => Number) { return x; }\n"
                          + "function send (x => Number) to (y => Number) { return x; }\n"
                          + "var a => Number;\nvar b => Number;\nvar result = send a to b;\n"));
}
