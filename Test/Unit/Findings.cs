// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Runtime;

namespace Unit;

/// <summary>
///     Spans, findings, and the renderer that turns one into a sentence.
/// </summary>
[Trait(nameof(Diagnostics), null)]
public class Findings
{
    /// <summary>
    ///     One of each kind, produced by the rules rather than assembled here.
    /// </summary>
    ///
    /// <remarks>
    ///     This used to build a single finding carrying every role and a related
    ///     span, which meant the golden file showed «first declared here» on a
    ///     reserved-word finding that has no prior site to point at — and no rule
    ///     had ever decided which span was related, because nothing but this
    ///     method populated one. Hand-built data standing in for the real path,
    ///     which is the same fault as a token chain standing in for source.
    /// </remarks>
    private static IEnumerable<Finding> Examples()
    {
        foreach (var source in new[]
                 {
                     "var total => Number;\nvar total => Number;\n",
                     "var old total => Number;\n",
                     """
                     function area of (radius => Number) { return radius; }
                     function area of (shape => Text) { return shape; }

                     """,
                     """
                     function b (x => Number) { return x; }
                     function b b (y => Number) { return y; }

                     """,
                     "function recall (x => Number) old (y => Number) { return x; }\n",
                     """
                     var hello to alice => Number;
                     function send (x => Number) to (y => Number) { return x; }

                     """,
                     """
                     var smoothed => Number;
                     function apply (x => Number) smoothed (y => Number) { return x; }

                     """,
                 })
        {
            SourceText text = new(source, "Player.ron");
            Lexer lexer = new(source);
            Parser parser = new(lexer.Lex());

            foreach (var finding in Ronin.Grammar.Declarations.Of(parser.Parse().Scopes[0].Statements, text).Problems)
            {
                yield return finding;
            }
        }

        // The cascade and initialisation rules take supplied data rather than
        // source, so their examples are supplied too — still produced by the
        // rule, which is what the totality test is asking.
        SourceText nowhere = new(string.Empty);
        Triggering when(string name) => new(name, nowhere.Span(0, 0));

        yield return Cascades.Diagnose(new Dictionary<Triggering, Effects>
        {
            [when("when ping arrives")] = new(new HashSet<string> { "pong count" }, new HashSet<string> { "ping count" }),
            [when("when pong arrives")] = new(new HashSet<string> { "ping count" }, new HashSet<string> { "pong count" }),
        }).Single();

        yield return Cascades.Writers(new Dictionary<Triggering, IReadOnlyCollection<Write>>
        {
            [when("when player dies")] = [new Write("game state", "when player dies")],
            [when("when timer expires")] = [new Write("game state", "when timer expires")],
        }).Single();

        yield return Initialisation.Diagnose(new Dictionary<Declared, IReadOnlySet<string>>
        {
            [new Declared("difficulty", nowhere.Span(0, 0))] = new HashSet<string> { "max health" },
            [new Declared("max health", nowhere.Span(0, 0))] = new HashSet<string> { "difficulty" },
        }).Single();
    }

    [Fact(DisplayName = "every kind renders")]
    public void EveryKindRenders()
    {
        // A kind that ships with no message is invisible until a user hits it,
        // so the enum itself is the test list — and every kind must be reachable
        // from a rule, or the golden file is describing something nothing emits.
        var examples = Examples().ToDictionary(finding => finding.Kind);

        foreach (var kind in Enum.GetValues<FindingKind>())
        {
            Assert.True(examples.ContainsKey(kind), $"{kind} is not produced by any rule");

            var rendered = Diagnostics.Render(examples[kind]);

            Assert.False(string.IsNullOrWhiteSpace(rendered), $"{kind} renders nothing");
            Assert.DoesNotContain("{", rendered);
        }
    }

    [Fact(DisplayName = "a reserved word points at nothing else")]
    public void AReservedWordPointsAtNothingElse()
    {
        // there is no prior declaration of «old total», so there is nowhere to
        // point, and pointing anywhere would be inventing a site
        var reserved = Examples().Single(finding => finding.Kind is FindingKind.ReservedPrefix);

        Assert.Empty(reserved.Related);
    }

    [Fact(DisplayName = "a report stays readable at the width of a cascade ring")]
    public void AReportStaysReadableAtTheWidthOfACascadeRing()
    {
        // A ring names every participant, so six related spans is the shape to
        // check before the cascade rules convert — one indented line each, and
        // the sentence stays on the first.
        SourceText source = new(string.Join("\n", Enumerable.Range(0, 7).Select(i => $"line {i}")));

        var finding = new Finding(FindingKind.Overloaded, source.Span(0, 4))
            .Naming("pattern", "area of (_)")
            .Naming("count", "7");

        foreach (var line in Enumerable.Range(1, 6)) finding.Alongside(source.Span(line * 7, 4), "also declared here");

        var lines = Diagnostics.Report(finding).Split(Environment.NewLine);

        Assert.Equal(7, lines.Length);
        Assert.StartsWith("source:1:1: «area of (_)»", lines[0]);
        Assert.All(lines.Skip(1), line => Assert.StartsWith("    source:", line));
        Assert.Equal("    source:7:1: also declared here", lines[^1]);
    }

    [Fact(DisplayName = "the wording of every kind, as a person reads it")]
    public void TheWordingOfEveryKind()
    {
        // The golden file. Rules assert structure, and wording lives here — so
        // improving a message shows up as a reviewable diff rather than as
        // nothing at all, and a rule test does not break when only the English
        // changed.
        var rendered = string.Join(Environment.NewLine + Environment.NewLine,
                                   Examples().Select(Diagnostics.Report));

        Assert.Equal(
            """
            Player.ron:2:5: «total» is already declared in this scope. Shadowing is not allowed, because reading a value has to tell you where it came from, and the compiler cannot flag the ambiguity when both readings are legal. Rename this one.
                Player.ron:1:5: first declared here

            Player.ron:1:5: «old total» begins with the reserved word «old», which is injected rather than declared. Respell it.

            Player.ron:1:10: «area of (_)» has 2 declarations and type-directed selection is not implemented, so there is no way to choose between them yet. Give them different shapes for now.
                Player.ron:2:10: also declared here

            Player.ron:2:10: the anchor of «b (_)» begins that of «b b (_)», so a statement can read as either and no bracketing tells them apart. Respell one of them.
                Player.ron:1:10: the anchor this one begins with

            Player.ron:1:10: «recall (_) old (_)» uses the reserved word «old» as a segment, which would make it glue and reject every injected name in scope. Respell that segment.

            Player.ron:1:5: «hello to alice» contains «to», which is glue in «send (_) to (_)». A name containing glue silently re-reads statements that already worked, so one of the two has to be respelled — and it is the later declaration that gives way.
                Player.ron:2:10: which makes it glue

            Player.ron:1:5: «old smoothed», injected by «smoothed», collides with pattern glue «smoothed» from «apply (_) smoothed (_)». Rename «smoothed», or respell the pattern.
                Player.ron:2:10: which makes it glue

            source:1:1: «when ping arrives» → «when pong arrives» → «when ping arrives» is a cycle: each writes something the next reads, so firing one schedules the next. Stop one of them writing what the ring reads, or declare feedback on every when in the ring.
                source:1:1: also in the ring

            source:1:1: «game state» is written by 2 whens. Whens fire in one round with no order between them, so one write would land and the other vanish. Derive the value instead, with a let that reads both conditions.
                source:1:1: also writes it

            source:1:1: «difficulty» → «max health» → «difficulty» is a cycle: each initialiser reads the one before it, so none of them can be evaluated first. Break the ring by giving one of them a value that does not depend on the others.
                source:1:1: also in the ring
            """,
            rendered);
    }

    [Fact(DisplayName = "a span knows its line and column")]
    public void ASpanKnowsItsLineAndColumn()
    {
        SourceText source = new("first\nsecond\nthird");

        Assert.Equal((1, 1), source.At(0));
        Assert.Equal((1, 5), source.At(4));
        Assert.Equal((2, 1), source.At(6));
        Assert.Equal((3, 4), source.At(16));

        // and a text with no path still describes itself
        Assert.Equal("source:2:1", source.Span(6, 6).ToString());
    }

    [Fact(DisplayName = "zero length is a position, not a range")]
    public void ZeroLengthIsAPositionNotARange()
    {
        // «expected a type after =>» points between two tokens, and forcing it to
        // cover one would make it point at the wrong character
        SourceText source = new("var x =>;");

        var caret = source.Span(8, 1).At;

        Assert.Equal(0, caret.Length);
        Assert.Equal(8, caret.Offset);
    }

    [Fact(DisplayName = "a declaration knows where it was written")]
    public void ADeclarationKnowsWhereItWasWritten()
    {
        // the whole path: source text, tokens carrying offsets, a span, a line
        const string text = "var base price => Number;\nvar base price => Number;\n";

        SourceText source = new(text, "Player.ron");
        Lexer lexer = new(text);
        Parser parser = new(lexer.Lex());

        var declared = Ronin.Grammar.Declarations.Of(parser.Parse().Scopes[0].Statements, source);
        var problem = Assert.Single(declared.Problems);

        Assert.Equal(FindingKind.Shadowed, problem.Kind);

        // the second declaration, which is the one that has to give way
        Assert.Equal((2, 5), source.At(problem.Primary.Offset));
        Assert.Equal("base price".Length, problem.Primary.Length);
    }
}
