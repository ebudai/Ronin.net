// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
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
