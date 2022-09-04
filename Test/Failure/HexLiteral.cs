using Ronin.Compiler;

namespace Failure;

public class HexLiteral
{
    [Fact(DisplayName = "doesn't start with 0x")]
    public void Fail()
    {
        const string literal = "not a hex literal";

        Lexer lexer = new(literal);
        var lexed = Ronin.Tokens.Literals.HexLiteral.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "unterminated")]
    public void Unterminated()
    {
        const string literal = "0x";

        Lexer lexer = new(literal);
        var lexed = Ronin.Tokens.Literals.HexLiteral.Lex(lexer);

        Assert.Null(lexed);
        Assert.NotNull(lexer.Error);
        Assert.NotEmpty(lexer.Error);
    }

    [Fact(DisplayName = "contains invalid chars")]
    public void Invalid()
    {
        const string literal = "0x1234g";

        Lexer lexer = new(literal);
        var lexed = Ronin.Tokens.Literals.HexLiteral.Lex(lexer);

        Assert.Null(lexed);
        Assert.NotNull(lexer.Error);
        Assert.NotEmpty(lexer.Error);
    }

    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Ronin.Tokens.Literals.HexLiteral.Lex(lexer);

        Assert.Null(lexed);
    }
}
