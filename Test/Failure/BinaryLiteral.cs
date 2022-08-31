using Ronin.Compiler;
using Ronin.Tokens;

namespace Failure;

public class BinaryLiteralFailureTests
{
    [Fact(DisplayName = "hex literal without 0x")]
    public void Fail()
    {
        const string literal = "not a binary literal";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = BinaryLiteral.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "unterminated binary literal")]
    public void Unterminated()
    {
        const string literal = "0b";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = BinaryLiteral.Lex(lexer);

        Assert.Null(lexed);
        Assert.NotNull(lexer.Error);
        Assert.NotEmpty(lexer.Error);
    }

    [Fact(DisplayName = "invalid char for binary literal")]
    public void Invalid()
    {
        const string literal = "0b101023";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = BinaryLiteral.Lex(lexer);

        Assert.Null(lexed);
        Assert.NotNull(lexer.Error);
        Assert.NotEmpty(lexer.Error);
    }
}
