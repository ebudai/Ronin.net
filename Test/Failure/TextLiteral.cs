using Ronin.Compiler;
using Ronin.Tokens.Literals;

namespace Failure;

public class TextLiteral
{
    [Fact(DisplayName = "lwithout quotes")]
    public void Fail()
    {
        const string literal = "testtest";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Literal.Text.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "unterminated")]
    public void Unterminated()
    {
        const string literal = "\"testtest";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Literal.Text.Lex(lexer);

        Assert.Null(lexed);
        Assert.NotNull(lexer.Error);
        Assert.NotEmpty(lexer.Error);
    }

    [Fact(DisplayName = "lone double quote")]
    public void SingleQuote()
    {
        const string literal = "\"";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Literal.Text.Lex(lexer);

        Assert.Null(lexed);
        Assert.NotNull(lexer.Error);
        Assert.NotEmpty(lexer.Error);
    }

    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Lexer lexer = new() { Sourcecode = string.Empty.ToArray() };
        var lexed = Literal.Text.Lex(lexer);

        Assert.Null(lexed);
    }
}