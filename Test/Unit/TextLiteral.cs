using Ronin.Compiler;
using Ronin.Tokens.Literals;

namespace Unit;

public class TextLiteral
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string literal = "\"testtest\"";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Literals.TextLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with escaped quotes")]
    public void Escaped()
    {
        const string literal = @"""tes\""tte\""st""";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Literals.TextLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }
}