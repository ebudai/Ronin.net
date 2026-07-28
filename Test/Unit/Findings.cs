// Copyright © 2026 Eric Budai

using Ronin.Compiler;

namespace Unit;

/// <summary>
///     Spans, findings, and the renderer that turns one into a sentence.
/// </summary>
[Trait(nameof(Diagnostics), null)]
public class Findings
{
    private static Finding Example(FindingKind kind)
    {
        SourceText source = new("var total => Number;\nvar total => Number;\n", "Player.ron");
        Finding finding = new(kind, source.Span(25, 5));

        // every role any renderer reads, so the totality test can build one of
        // each kind without knowing which roles it needs
        return finding
            .Naming("name", "total")
            .Naming("where", "in an enclosing scope")
            .Naming("word", "old")
            .Naming("pattern", "area of (_)")
            .Naming("count", "2")
            .Alongside(source.Span(4, 5), "first declared here");
    }

    [Fact(DisplayName = "every kind renders")]
    public void EveryKindRenders()
    {
        // A kind that ships with no message is invisible until a user hits it,
        // so the enum itself is the test list.
        foreach (var kind in Enum.GetValues<FindingKind>())
        {
            var rendered = Diagnostics.Render(Example(kind));

            Assert.False(string.IsNullOrWhiteSpace(rendered), $"{kind} renders nothing");
            Assert.DoesNotContain("{", rendered);
        }
    }

    [Fact(DisplayName = "the wording of every kind, as a person reads it")]
    public void TheWordingOfEveryKind()
    {
        // The golden file. Rules assert structure, and wording lives here — so
        // improving a message shows up as a reviewable diff rather than as
        // nothing at all, and a rule test does not break when only the English
        // changed.
        var rendered = string.Join(Environment.NewLine + Environment.NewLine,
                                   Enum.GetValues<FindingKind>().Select(kind => Diagnostics.Report(Example(kind))));

        Assert.Equal(
            """
            Player.ron:2:5: «total» is already declared in an enclosing scope. Shadowing is not allowed, because reading a value has to tell you where it came from, and the compiler cannot flag the ambiguity when both readings are legal. Rename this one.
                Player.ron:1:5: first declared here

            Player.ron:2:5: «total» begins with the reserved word «old», which is injected rather than declared. Respell it.
                Player.ron:1:5: first declared here

            Player.ron:2:5: «area of (_)» has 2 declarations and type-directed selection is not implemented, so there is no way to choose between them yet. Give them different shapes for now.
                Player.ron:1:5: first declared here
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
