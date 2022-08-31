using Ronin.Compiler;
using Ronin.Tokens;

namespace Unit;

public class TextLiteralUnitTests
{
    [Fact(DisplayName = "parse basic text literal")]
    public void Basic()
    {
        const string literal = "\"testtest\"";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = TextLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length, lexed.SourceIndex);
    }

    [Fact(DisplayName = "parse text literal with escaped quotes")]
    public void Escaped()
    {
        const string literal = @"""tes\""tte\""st""";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = TextLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length, lexed.SourceIndex);
    }
}