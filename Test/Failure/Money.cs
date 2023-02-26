using Ronin.Compiler;
using Ronin.Lexicon;

namespace Failure;

[Trait("Lexer", null)]
public class Money
{
    [Fact(DisplayName = "doesn't start with a dollar sign")]
    public void DoesntStartWithADollarSign()
    {
        const string number = "987.23";

        Lexer lexer = new(number);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<MoneyLiteral>(lexed);
    }

    [Fact(DisplayName = "doesn't continue with a number")]
    public void DoesntContinueWithANumber()
    {
        const string literal = "$f987.23";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "can't end with dot")]
    public void EndsWithDot()
    {
        const string literal = "$9.";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as MoneyLiteral;

        Assert.Equal(literal[..^1], money.ToString());
    }

    [Fact(DisplayName = "contains invalid chars")]
    public void Invalid()
    {
        const string literal = "$9.2v5";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as MoneyLiteral;

        Assert.Equal(literal[..^2], money.ToString());
    }

    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Literal.Lex(ref lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "multiple dots")]
    public void MultipleDots()
    {
        const string literal = "$9.25.4";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as MoneyLiteral;

        Assert.Equal(literal[..^2], money.ToString());
    }

    [Fact(DisplayName = "just a dollar sign")]
    public void JustADollarSign()
    {
        const string literal = "$";

        Lexer lexer = new(literal);
        var value = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(value);
    }
}
