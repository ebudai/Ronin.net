// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Runtime;
using Test;

namespace Unit;

/// <summary>
///     Text literals and the token sequence, against real source.
/// </summary>
[Trait(nameof(Lexer), null)]
public class TextEscapes
{
    private static IEnumerable<string> Lex(string source)
    {
        Lexer lexer = new(source);
        return lexer.Lex().ToLexemes().Select(lexeme => lexeme.Text);
    }

    [Fact(DisplayName = "an escaped backslash does not escape the quote after it")]
    public void AnEscapedBackslashDoesNotEscapeTheQuoteAfterIt()
    {
        // «"a\\"» is a text holding one backslash, and the quote closes it.
        // Looking only at the previous character read that as an escaped quote
        // and ran on to whatever came next.
        Assert.Equal([@"""a\\""", ";"], Lex(@"""a\\"";"));

        // an escaped quote genuinely does not close it
        Assert.Equal([@"""a\""b""", ";"], Lex(@"""a\""b"";"));

        // and the plain case still works
        Assert.Equal([@"""ab""", ";"], Lex(@"""ab"";"));
    }

    [Fact(DisplayName = "an escape means something by the time it is a value")]
    public void AnEscapeMeansSomethingByTheTimeItIsAValue()
    {
        // The lexer goes to real trouble to recognise these, and the evaluator
        // stripped the quotes and nothing else — so every escape survived into
        // the value and «"a\""» was four characters ending in backslash-quote
        // rather than two ending in a quote.
        Assert.Equal(@"a""b", Value(@"""a\""b"""));
        Assert.Equal(@"a\", Value(@"""a\\"""));
        Assert.Equal("ab", Value(@"""ab"""));
        Assert.Equal(string.Empty, Value(@""""""));
    }

    [Fact(DisplayName = "an escape the language does not have is an error, not a backslash")]
    public void AnEscapeTheLanguageDoesNotHaveIsAnErrorNotABackslash()
    {
        // «\n» has no meaning yet. Passing it through as backslash-n is what
        // would stop it ever meaning a newline, because by then programs would
        // depend on the literal two characters.
        var refused = Assert.IsType<Error>(Value(@"""line\nbreak"""));

        Assert.Contains(@"«\n» is not an escape", refused.Message);
    }

    private static object Value(string literal)
    {
        SymbolTable symbols = new();
        Resolver resolver = new(symbols);

        Assert.True(resolver.Resolve(Lexemes.Lex(literal)).TryTree(out var tree), literal);

        return new Evaluator(new Ronin.Runtime.Scope()).Evaluate(new Graph(), tree, insideLet: false);
    }

    [Fact(DisplayName = "a running index is an offset, not a token count")]
    public void ARunningIndexIsAnOffsetNotATokenCount()
    {
        // ReadOnlySequenceSegment defines RunningIndex as the offset of the
        // segment within the sequence, so every SequencePosition depended on it
        Lexer lexer = new("var price;");
        var tokens = lexer.Lex().ToArray();

        var offset = 0L;
        foreach (var token in tokens)
        {
            Assert.Equal(offset, token.RunningIndex);
            offset += token.Memory.Length;
        }
    }
}
