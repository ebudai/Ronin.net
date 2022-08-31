using Ronin.Compiler;
using Ronin.Tokens;

namespace Failure;

public class CharLiteralFailureTests
{
    [Fact(DisplayName = "lex char literal without single quotes")]
    public void Fail()
    {
        const string literal = "testtest";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = CharLiteral.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "unterminated char literal")]
    public void Unterminated()
    {
        const string literal = "'c";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = CharLiteral.Lex(lexer);

        Assert.Null(lexed);
        Assert.NotNull(lexer.Error);
        Assert.NotEmpty(lexer.Error);
    }

    [Fact(DisplayName = "empty char literal")]
    public void Empty()
    {
        const string literal = "''";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = CharLiteral.Lex(lexer);

        Assert.Null(lexed);
        Assert.NotNull(lexer.Error);
        Assert.NotEmpty(lexer.Error);
    }

    [Fact(DisplayName = "multi char literal")]
    public void TooMany()
    {
        const string literal = "'test'";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = CharLiteral.Lex(lexer);

        Assert.Null(lexed);
        Assert.NotNull(lexer.Error);
        Assert.NotEmpty(lexer.Error);
    }
}
