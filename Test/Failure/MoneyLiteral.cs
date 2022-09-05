using Ronin.Compiler;
using Ronin.Token;

namespace Failure;

public class MoneyLiteral
{
    [Fact(DisplayName = "doesn't start with a dollar sign")]
    public void DoesntStartWithADollarSign()
    {
        const string number = "987.23";

        Lexer lexer = new(number);
        var lexed = Literal.Lex(lexer);

        Assert.IsType<Literal>(lexed);

        var literal = lexed as Literal;

        Assert.NotEqual(Literal.Kind.money, literal.LiteralKind);
    }

    [Fact(DisplayName = "doesn't continue with a number")]
    public void DoesntContinueWithANumber()
    {
        const string literal = "$f987.23";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "unterminated")]
    public void Unterminated()
    {
        const string literal = "$9.";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "contains invalid chars")]
    public void Invalid()
    {
        const string literal = "$9.2v5";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "multiple dots")]
    public void MultipleDots()
    {
        const string literal = "$9.25.4";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
        Assert.NotNull(lexer.Error);
        Assert.NotEmpty(lexer.Error);
    }

    [Fact(DisplayName = "just a dollar sign")]
    public void JustADollarSign()
    {
        const string literal = "$";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
        Assert.NotNull(lexer.Error);
        Assert.NotEmpty(lexer.Error);
    }
}
