using Ronin.Compiler;
using Ronin.Tokens;

namespace Failure;

public class TextLiteralFailureTests
{
    [Fact(DisplayName = "lex text literal without quotes")]
    public void Fail()
    {
        const string literal = "testtest";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = TextLiteral.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "lex unterminated text literal")]
    public void Unterminated()
    {
        const string literal = "\"testtest";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = TextLiteral.Lex(lexer);

        Assert.Null(lexed);
        Assert.NotNull(lexer.Error);
        Assert.NotEmpty(lexer.Error);
    }

    [Fact(DisplayName = "lex single quote unterminated text literal")]
    public void SingleQuote()
    {
        const string literal = "\"";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = TextLiteral.Lex(lexer);

        Assert.Null(lexed);
        Assert.NotNull(lexer.Error);
        Assert.NotEmpty(lexer.Error);
    }
}