using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Literals;

namespace Failure;

public class MoneyLiteral
{
    [Fact(DisplayName = "doesn't start with a dollar sign")]
    public void DoesntStartWithADollarSign()
    {
        const string number = "987.23";

        Lexer lexer = new(number);
        var lexed = Literal.Lex(lexer);

        Assert.IsNotType<Money>(lexed);
    }

    [Fact(DisplayName = "doesn't continue with a number")]
    public void DoesntContinueWithANumber()
    {
        const string literal = "$f987.23";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "can't end with dot")]
    public void EndsWithDot()
    {
        const string literal = "$9.";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.IsType<Money>(lexed);
        var money = lexed as Money;
        Assert.Equal("$9".ToArray(), money.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "contains invalid chars")]
    public void Invalid()
    {
        const string literal = "$9.2v5";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.IsType<Money>(lexed);
        var money = lexed as Money;
        Assert.Equal("$9.2".ToArray(), money.Sourcecode.ToArray());
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

        Assert.NotNull(lexed);
        Assert.IsType<Money>(lexed);
        var money = lexed as Money;
        Assert.Equal("$9.25".ToArray(), money.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "just a dollar sign")]
    public void JustADollarSign()
    {
        const string literal = "$";

        Lexer lexer = new(literal);
        var lexed = lexer.Lex();

        Assert.NotEmpty(lexed);
        Assert.IsType<Ronin.Lexicon.Word>(lexed[0]);
        var name = lexed[0] as Ronin.Lexicon.Word;
        Assert.Equal(literal.ToArray(), name.Sourcecode.ToArray());
    }
}
