using Ronin.Compiler;
using Ronin.Token;

namespace Failure;

public class CharLiteral
{
    [Fact(DisplayName = "no single quotes")]
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
        const string literal = "'c";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.IsType<Error>(lexed);
        var error = lexed as Error;
        Assert.Equal(literal.ToArray(), error.Sourcecode.ToArray());
        Assert.Equal("unterminated character literal", error.Message);
    }

    [Fact(DisplayName = "empty")]
    public void Empty()
    {
        const string literal = "''";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.IsType<Error>(lexed);
        var error = lexed as Error;
        Assert.Equal(literal.ToArray(), error.Sourcecode.ToArray());
        Assert.Equal("empty character literal", error.Message);
    }

    [Fact(DisplayName = "contains multiple chars")]
    public void TooMany()
    {
        const string literal = "'test'";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.IsType<Error>(lexed);
        var error = lexed as Error;
        Assert.Equal(literal.ToArray(), error.Sourcecode.ToArray());
        Assert.Equal("bad unicode literal", error.Message);
    }

    [Fact(DisplayName = "unichar with bad contents")]
    public void BadUnichar()
    {
        const string literal = "'\\uABH7'";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);
    }

    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }
}
