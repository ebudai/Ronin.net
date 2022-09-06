using Ronin.Compiler;
using Ronin.Token;

namespace Failure;

public class BinaryLiteral
{
    [Fact(DisplayName = "doesn't start with 0x")]
    public void Fail()
    {
        const string literal = "not a binary literal";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "unterminated")]
    public void Unterminated()
    {
        const string literal = "0b";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.IsType<Error>(lexed);
        var error = lexed as Error;
        Assert.Equal(literal.ToArray(), error.Sourcecode.ToArray());
        Assert.Equal("unterminated binary literal", error.Message);
    }

    [Fact(DisplayName = "contains invalid char")]
    public void Invalid()
    {
        const string literal = "0b101023";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.IsType<Error>(lexed);
        var error = lexed as Error;
        Assert.Equal(literal[..^2].ToArray(), error.Sourcecode.ToArray());
        Assert.Equal("invalid char '2' at 6 for binary literal", error.Message);
    }

    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }
}
