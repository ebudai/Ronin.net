using Ronin.Compiler;

namespace Failure;

public class IntegerLiteral
{
    [Fact(DisplayName = "doesn't start with a number")]
    public void DoesntStartWithANumber()
    {
        const string literal = "g98723";

        Lexer lexer = new(literal);
        var lexed = Ronin.Tokens.Literals.IntegerLiteral.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "contains invalid chars")]
    public void Invalid()
    {
        const string literal = "92v5";

        Lexer lexer = new(literal);
        var lexed = Ronin.Tokens.Literals.IntegerLiteral.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Ronin.Tokens.Literals.IntegerLiteral.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "contains a dot")]
    public void Dot()
    {
        const string literal = "98723.2";

        Lexer lexer = new(literal);
        var lexed = Ronin.Tokens.Literals.IntegerLiteral.Lex(lexer);

        Assert.Null(lexed);
    }
}
