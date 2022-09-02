using Ronin.Compiler;

namespace Failure;

public class CharLiteral
{
    [Fact(DisplayName = "no single quotes")]
    public void Fail()
    {
        const string literal = "testtest";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Literals.CharLiteral.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "unterminated")]
    public void Unterminated()
    {
        const string literal = "'c";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Literals.CharLiteral.Lex(lexer);

        Assert.Null(lexed);
        Assert.NotNull(lexer.Error);
        Assert.NotEmpty(lexer.Error);
    }

    [Fact(DisplayName = "empty")]
    public void Empty()
    {
        const string literal = "''";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Literals.CharLiteral.Lex(lexer);

        Assert.Null(lexed);
        Assert.NotNull(lexer.Error);
        Assert.NotEmpty(lexer.Error);
    }

    [Fact(DisplayName = "contains multiple chars")]
    public void TooMany()
    {
        const string literal = "'test'";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Literals.CharLiteral.Lex(lexer);

        Assert.Null(lexed);
        Assert.NotNull(lexer.Error);
        Assert.NotEmpty(lexer.Error);
    }

    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Lexer lexer = new() { Sourcecode = string.Empty.ToArray() };
        var lexed = Ronin.Tokens.Literals.CharLiteral.Lex(lexer);

        Assert.Null(lexed);
    }
}
