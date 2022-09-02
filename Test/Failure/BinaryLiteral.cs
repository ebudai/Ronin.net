using Ronin.Compiler;

namespace Failure;

public class BinaryLiteral
{
    [Fact(DisplayName = "doesn't start with 0x")]
    public void Fail()
    {
        const string literal = "not a binary literal";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Literals.BinaryLiteral.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "unterminated")]
    public void Unterminated()
    {
        const string literal = "0b";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Literals.BinaryLiteral.Lex(lexer);

        Assert.Null(lexed);
        Assert.NotNull(lexer.Error);
        Assert.NotEmpty(lexer.Error);
    }

    [Fact(DisplayName = "contains invalid char")]
    public void Invalid()
    {
        const string literal = "0b101023";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Literals.BinaryLiteral.Lex(lexer);

        Assert.Null(lexed);
        Assert.NotNull(lexer.Error);
        Assert.NotEmpty(lexer.Error);
    }

    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Lexer lexer = new() { Sourcecode = string.Empty.ToArray() };
        var lexed = Ronin.Tokens.Literals.BinaryLiteral.Lex(lexer);

        Assert.Null(lexed);
    }
}
