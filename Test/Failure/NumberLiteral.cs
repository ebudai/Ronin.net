using Ronin.Compiler;
using Ronin.Token;

namespace Failure;

public class NumberLiteral
{
    [Fact(DisplayName = "doesn't start with a number")]
    public void DoesntStartWithANumber()
    {
        const string literal = "g987.23";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "doesn't have a .")]
    public void DoesntHaveADot()
    {
        const string integer = "98723";

        Lexer lexer = new(integer);
        var lexed = Literal.Lex(lexer);

        Assert.IsType<Literal>(lexed);

        var literal = lexed as Literal;

        Assert.NotEqual(Literal.Kind.number, literal.LiteralKind);
    }

    [Fact(DisplayName = "unterminated")]
    public void Unterminated()
    {
        const string literal = "9.";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.IsType<Error>(lexed);
        var error = lexed as Error;
        Assert.Equal(literal.ToArray(), error.Sourcecode.ToArray());
        Assert.Equal("unterminated number literal", error.Message);
    }

    [Fact(DisplayName = "contains invalid chars")]
    public void Invalid()
    {
        const string literal = "9.2v5";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.IsType<Error>(lexed);
        var error = lexed as Error;
        Assert.Equal("9.2".ToArray(), error.Sourcecode.ToArray());
        Assert.Equal("number literal with non-numeric character 'v' at 3", error.Message);
    }

    [Fact(DisplayName = "contains multiple dots")]
    public void MultipleDots()
    {
        const string literal = "9.2.5";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.IsType<Error>(lexed);
        var error = lexed as Error;
        Assert.Equal("9.2".ToArray(), error.Sourcecode.ToArray());
        Assert.Equal("number literal with multiple dots", error.Message);
    }

    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }
}
