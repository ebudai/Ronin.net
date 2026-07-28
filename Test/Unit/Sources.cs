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

    [Theory(DisplayName = "a comma between digits is part of the number")]
    [InlineData("1,234", "1,234")]
    [InlineData("7,000,876", "7,000,876")]
    [InlineData("1,234.56", "1,234.56")]
    [InlineData("2345", "2345")]                 // a bare run is a number however long
    [InlineData("1,2345", "1")]                  // second group is not three digits
    [InlineData("1,234,56", "1,234")]            // longest well-formed prefix
    [InlineData("1,", "1")]                      // a trailing comma is never part of it
    [InlineData("1,,234", "1")]                  // an empty group is not a group
    [InlineData("1234,567", "1234")]             // a first group over three digits
    public void ACommaBetweenDigitsIsPartOfTheNumber(string source, string number)
        => Assert.Equal(number, Lex(source).First());

    [Fact(DisplayName = "a spaced comma is two things, an unspaced one is one")]
    public void ASpacedCommaIsTwoThingsAnUnspacedOneIsOne()
    {
        // how both are already written by hand, which is what makes the rule
        // make the reader right rather than asking them to learn anything
        Assert.Equal(["1,234"], Lex("1,234"));
        Assert.Equal(["1", ",", "234"], Lex("1, 234"));

        // so a call cannot change arity when a constant is inlined into it
        Assert.Equal(["f", "(", "1,234", ")"], Lex("f(1,234)"));
        Assert.Equal(["f", "(", "1", ",", "234", ")"], Lex("f(1, 234)"));
    }

    [Fact(DisplayName = "an unspaced separator is a parse error, not a symbol")]
    public void AnUnspacedSeparatorIsAParseErrorNotASymbol()
    {
        // Declining to lex it as a separator would leave a bare symbol that
        // Symbolic absorbs into the reference beside it, so «f(a,b)» would become
        // one argument holding a stray comma rather than an error.
        Lexer lexer = new("(a,b)");
        Parser parser = new(lexer.Lex());

        Assert.Null(Ronin.Grammar.Inputs.Parse(ref parser));
    }

    [Fact(DisplayName = "a decimal is distinguished at the lexer")]
    public void ADecimalIsDistinguishedAtTheLexer()
    {
        // purely lexical — the presence of a «.» — so nothing here needs the
        // symbol table
        Lexer whole = new("42");
        Assert.False(((Numeric)Numeric.Lex(ref whole)).IsDecimal);

        Lexer fraction = new("4.5");
        Assert.True(((Numeric)Numeric.Lex(ref fraction)).IsDecimal);

        // a lone dot after a number is not part of it
        Assert.Equal(["7", "."], Lex("7."));
    }

    [Fact(DisplayName = "a multiline comment does not disturb what follows")]
    public void AMultilineCommentDoesNotDisturbWhatFollows()
    {
        Assert.Equal(["x", ";", "y", ";"], Lex("x; /* note */ y;"));
    }
}
