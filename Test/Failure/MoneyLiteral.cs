using Ronin.Compiler;
using Ronin.Tokens.Literals;

namespace Failure;

public class MoneyLiteral
{
    [Fact(DisplayName = "doesn't start with a dollar sign")]
    public void DoesntStartWithADollarSign()
    {
        const string literal = "987.23";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Literal.Money.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "doesn't continue with a number")]
    public void DoesntContinueWithANumber()
    {
        const string literal = "$f987.23";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Literal.Money.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "unterminated")]
    public void Unterminated()
    {
        const string literal = "$9.";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Literal.Money.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "contains invalid chars")]
    public void Invalid()
    {
        const string literal = "$9.2v5";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Literal.Money.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Lexer lexer = new() { Sourcecode = string.Empty.ToArray() };
        var lexed = Literal.Money.Lex(lexer);

        Assert.Null(lexed);
    }
}
