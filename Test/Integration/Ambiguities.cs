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

    [Fact(DisplayName = "and an unambiguous file says nothing")]
    public void AndAnUnambiguousFileSaysNothing()
        // The same statement with the colliding name gone. Without it there is
        // one reading, and a rule that fired anyway would be refusing the
        // language rather than an ambiguity in it.
        => Assert.Empty(All("function send (x => Number) { return x; }\n"
                          + "function send (x => Number) to (y => Number) { return x; }\n"
                          + "var a => Number;\nvar b => Number;\nvar result = send a to b;\n"));
}
