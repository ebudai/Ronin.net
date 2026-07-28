// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Unit;

/// <summary>
///     The lexer against real source text.
/// </summary>
///
/// <remarks>
///     Every defect here was found by an audit rather than by the suite, and all
///     three share one cause: tests above the lexer hand-built token chains, so
///     nothing ever asked what the lexer does with a file. The rule that follows
///     is that a test may hand-build tokens only when token construction is the
///     thing under test.
/// </remarks>
[Trait(nameof(Lexer), null)]
public class Sources
{
    private static IEnumerable<string> Lex(string source)
    {
        Lexer lexer = new(source);
        return lexer.Lex().ToLexemes().Select(lexeme => lexeme.Text);
    }

    [Fact(DisplayName = "an empty source is a sentinel, not a null")]
    public void AnEmptySourceIsASentinelNotANull()
    {
        Lexer lexer = new(string.Empty);
        var tokens = lexer.Lex();

        Assert.IsType<Sentinel>(tokens);

        // and the parser can therefore be handed one
        Parser parser = new(tokens);
        Assert.Empty(parser.Parse().Scopes[0].Statements);
    }

    [Fact(DisplayName = "a keyword may end the source")]
    public void AKeywordMayEndTheSource()
    {
        // reading one character past the keyword to look for a boundary threw
        // for any file ending in one
        Assert.Equal(["if"], Lex("if"));
        Assert.Equal(["var"], Lex("var"));
        Assert.Equal(["compiled"], Lex("compiled"));
        Assert.Equal(["constant"], Lex("constant"));
    }

    [Fact(DisplayName = "a keyword still needs a boundary")]
    public void AKeywordStillNeedsABoundary()
    {
        Lexer lexer = new("iffy");
        Assert.Null(If.Lex(ref lexer));

        Lexer boundary = new("if x");
        Assert.NotNull(If.Lex(ref boundary));
    }

    [Fact(DisplayName = "a comment ends at its line, wherever the line is")]
    public void ACommentEndsAtItsLineWhereverTheLineIs()
    {
        // Lexer.IndexOf returned an absolute index and Comment.Lex advanced by it
        // as a length, so a comment anywhere but the start of a file consumed
        // everything after it. «y ;» simply vanished.
        Assert.Equal(["x", ";", "y", ";"], Lex("x; // note\ny;"));

        // still correct at the start, which is why it went unnoticed
        Assert.Equal(["y", ";"], Lex("// note\ny;"));

        // and a comment reaching the end of the source has no newline to find
        Assert.Equal(["x", ";"], Lex("x; // note"));
    }

    [Fact(DisplayName = "a multiline comment does not disturb what follows")]
    public void AMultilineCommentDoesNotDisturbWhatFollows()
    {
        Assert.Equal(["x", ";", "y", ";"], Lex("x; /* note */ y;"));
    }
}
