using Ronin.Compiler;
using Ronin.Token;

namespace Failure;

public class HexLiteral
{
    [Fact(DisplayName = "doesn't start with 0x")]
    public void Fail()
    {
        const string literal = "not a hex literal";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "unterminated")]
    public void Unterminated()
    {
        const string literal = "0x";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.IsType<Error>(lexed);
        var error = lexed as Error;
        Assert.Equal(literal.ToArray(), error.Sourcecode.ToArray());
        Assert.Equal("unterminated hex literal", error.Message);
    }

    [Fact(DisplayName = "contains invalid chars")]
    public void Invalid()
    {
        const string literal = "0x1234g";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.IsType<Error>(lexed);
        var error = lexed as Error;
        Assert.Equal(literal[..^1].ToArray(), error.Sourcecode.ToArray());
        Assert.Equal("invalid character 'g' at 6 for hex literal", error.Message);
    }

    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }
}
