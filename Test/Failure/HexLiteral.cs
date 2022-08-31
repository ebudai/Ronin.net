using Ronin.Compiler;
using Ronin.Tokens;

namespace Failure;

public class HexLiteralFailureTests
{
    [Fact(DisplayName = "hex literal without 0x")]
    public void Fail()
    {
        const string literal = "not a hex literal";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = HexLiteral.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "unterminated hex literal")]
    public void Unterminated()
    {
        const string literal = "0x";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = HexLiteral.Lex(lexer);

        Assert.Null(lexed);
        Assert.NotNull(lexer.Error);
        Assert.NotEmpty(lexer.Error);
    }

    [Fact(DisplayName = "invalid char for hex literal")]
    public void Invalid()
    {
        const string literal = "0x1234g";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = HexLiteral.Lex(lexer);

        Assert.Null(lexed);
        Assert.NotNull(lexer.Error);
        Assert.NotEmpty(lexer.Error);
    }


}
