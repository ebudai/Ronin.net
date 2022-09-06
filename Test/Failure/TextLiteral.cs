using Ronin.Compiler;
using Ronin.Token;

namespace Failure;

public class TextLiteral
{
    [Fact(DisplayName = "without quotes")]
    public void Fail()
    {
        const string literal = "testtest";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "unterminated")]
    public void Unterminated()
    {
        const string literal = "\"testtest";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.IsType<Error>(lexed);
        var error = lexed as Error;
        Assert.Equal(literal.ToArray(), error.Sourcecode.ToArray());
        Assert.Equal("unterminated text literal", error.Message);
    }

    [Fact(DisplayName = "lone double quote")]
    public void SingleQuote()
    {
        const string literal = "\"";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.IsType<Error>(lexed);
        var error = lexed as Error;
        Assert.Equal(literal.ToArray(), error.Sourcecode.ToArray());
        Assert.Equal("unterminated text literal", error.Message);
    }

    [Fact(DisplayName = "tricky unterminated")]
    public void TrickyUnterminated()
    {
        const string literal = "\"this is text\\\" unterminated";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.IsType<Error>(lexed);
        var error = lexed as Error;
        Assert.Equal(literal.ToArray(), error.Sourcecode.ToArray());
        Assert.Equal("unterminated text literal", error.Message);
    }

    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }
}