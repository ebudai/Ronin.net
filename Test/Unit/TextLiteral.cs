using Ronin.Compiler;

namespace Unit;

public class TextLiteral
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string literal = "\"testtest\"";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.TextLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length, lexed.SourceIndex);
    }

    [Fact(DisplayName = "with escaped quotes")]
    public void Escaped()
    {
        const string literal = @"""tes\""tte\""st""";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.TextLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length, lexed.SourceIndex);
    }
}