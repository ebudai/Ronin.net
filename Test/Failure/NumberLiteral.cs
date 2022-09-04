using Ronin.Compiler;

namespace Failure;

public class NumberLiteral
{
    [Fact(DisplayName = "doesn't start with a number")]
    public void DoesntStartWithANumber()
    {
        const string literal = "g987.23";

        Lexer lexer = new(literal);
        var lexed = Ronin.Tokens.Literals.NumberLiteral.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "doesn't have a .")]
    public void DoesntHaveADot()
    {
        const string literal = "98723";

        Lexer lexer = new(literal);
        var lexed = Ronin.Tokens.Literals.NumberLiteral.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "unterminated")]
    public void Unterminated()
    {
        const string literal = "9.";

        Lexer lexer = new(literal);
        var lexed = Ronin.Tokens.Literals.NumberLiteral.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "contains invalid chars")]
    public void Invalid()
    {
        const string literal = "9.2v5";

        Lexer lexer = new(literal);
        var lexed = Ronin.Tokens.Literals.NumberLiteral.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "contains multiple dots")]
    public void MultipleDots()
    {
        const string literal = "9.2.5";

        Lexer lexer = new(literal);
        var lexed = Ronin.Tokens.Literals.NumberLiteral.Lex(lexer);

        Assert.Null(lexed);
        Assert.NotNull(lexer.Error);
        Assert.NotEmpty(lexer.Error);
    }

    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Ronin.Tokens.Literals.NumberLiteral.Lex(lexer);

        Assert.Null(lexed);
    }
}
