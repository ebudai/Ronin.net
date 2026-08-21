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
///     Every expectation in <c>Resolutions</c> was verified against the Python
///     reference through a splitter written beside <c>Lexeme</c>, so those tests
///     said something about the compiler only by agreement with a second lexer.
///     The splitter is gone and they go through this, so the agreement is now the
///     absence of anything to disagree with.
/// </remarks>
[Trait(nameof(Resolver), null)]
public class Adaptations : ParsingTests
{
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
        // all three carry the one LexemeKind.Literal.
        var lexemes = Lexemes.Lex("42 2023-11-16 \"text\"");

        Assert.Equal(new[] { LexemeKind.Literal, LexemeKind.Literal, LexemeKind.Literal },
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
        //
        // «base», «base price» and the pattern «base (_)» all fit the same span,
        // so this is three readings rather than the cheapest of them. It used to
        // be one, silently — maximal munch by cost — and the readings are
        // ordered by cost still, which is what puts the intended one first in
        // the message.
        SymbolTable symbols = new();
        symbols.WithNames("base", "base price", "price", "tax").WithPatterns("base _");

        Resolver resolver = new(symbols);
        var resolution = resolver.Resolve(Lexemes.Lex("base price + tax"));

        Assert.Equal("Ambiguous", resolution.Kind.ToString());
        Assert.Equal(2, resolution.Cost);
        Assert.Equal("(«base price» + «tax»)", resolution.Reading);
    }
}
