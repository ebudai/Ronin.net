// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using Test;

namespace Unit;

/// <summary>
///     The adapter between <see cref="Lexer"/> and <see cref="Resolver"/>.
/// </summary>
///
/// <remarks>
///     The load-bearing test is <see cref="AgreesWithTheSplitter"/>. Every
///     expectation in <c>Resolutions</c> was verified against the Python reference
///     through <c>Lexeme.Split</c>, so those 23 tests only say something about the
///     real compiler if the real lexer produces the same lexemes the splitter does.
/// </remarks>
[Trait(nameof(Resolver), null)]
public class Adaptations : ParsingTests
{
    [Theory(DisplayName = "agrees with the splitter")]
    [InlineData("base price + tax")]
    [InlineData("sum of list")]
    [InlineData("send hello to alice")]
    [InlineData("print sum of sum of list")]
    [InlineData("compute total for order")]
    [InlineData("send the report today")]
    [InlineData("send a + b to c")]
    [InlineData("compute total for a + b")]
    [InlineData("compute total for (a) + b")]
    [InlineData("(compute total for a) + b")]
    [InlineData("print a + b * c")]
    [InlineData("print 42")]
    [InlineData("sum of (list)")]
    [InlineData("sum (of list)")]
    [InlineData("compute total for (a + b)")]
    [InlineData("data + sum of x")]
    public void AgreesWithTheSplitter(string source)
        => Assert.Equal(Lexeme.Split(source), Lexemes.Lex(source));

    [Fact(DisplayName = "brackets are open and close, not symbols")]
    public void BracketsAreOpenAndClose()
    {
        var lexemes = Lexemes.Lex("(a) [b] {c}");

        Assert.Equal(
            new[]
            {
                LexemeKind.Open, LexemeKind.Word, LexemeKind.Close,
                LexemeKind.Open, LexemeKind.Word, LexemeKind.Close,
                LexemeKind.Open, LexemeKind.Word, LexemeKind.Close,
            },
            lexemes.Select(lexeme => lexeme.Kind));
    }

    [Fact(DisplayName = "every literal is a free atom")]
    public void EveryLiteralIsAFreeAtom()
    {
        // Date and Text cost no lookup for the same reason Numeric does not, so
        // they carry the same kind despite that kind being spelled Number.
        var lexemes = Lexemes.Lex("42 2023-11-16 \"text\"");

        Assert.Equal(new[] { LexemeKind.Number, LexemeKind.Number, LexemeKind.Number },
                     lexemes.Select(lexeme => lexeme.Kind));
        Assert.Equal(new[] { "42", "2023-11-16", "\"text\"" },
                     lexemes.Select(lexeme => lexeme.Text));
    }

    [Fact(DisplayName = "a keyword is a word")]
    public void AKeywordIsAWord()
    {
        var lexemes = Lexemes.Lex("if x");

        Assert.Equal(new[] { LexemeKind.Word, LexemeKind.Word }, lexemes.Select(lexeme => lexeme.Kind));
    }

    [Fact(DisplayName = "trivia is dropped and the sentinel terminates")]
    public void TriviaIsDroppedAndTheSentinelTerminates()
    {
        List<Token> tokens =
        [
            Word("a"),
            Whitespace(),
            Word("b"),
            new Sentinel(),
            Word("unreachable"),
        ];

        var lexemes = tokens.AsLinkedList().ToLexemes();

        Assert.Equal(new[] { "a", "b" }, lexemes.Select(lexeme => lexeme.Text));
    }

    [Fact(DisplayName = "an empty source adapts to nothing")]
    public void AnEmptySourceAdaptsToNothing() => Assert.Empty(Lexemes.Lex(string.Empty));

    [Fact(DisplayName = "resolves what the lexer actually produced")]
    public void ResolvesWhatTheLexerActuallyProduced()
    {
        // End to end through the real lexer: the resolver never sees a string.
        SymbolTable symbols = new();
        symbols.WithNames("base", "base price", "price", "tax").WithPatterns("base _");

        Resolver resolver = new(symbols);
        var resolution = resolver.Resolve(Lexemes.Lex("base price + tax"));

        Assert.Equal("Resolved", resolution.Kind.ToString());
        Assert.Equal(2, resolution.Cost);
        Assert.Equal("(«base price» + «tax»)", resolution.Reading);
    }
}
