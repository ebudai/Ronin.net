using Ronin.Compiler;

namespace Failure;

public class Symbol
{
    [Fact(DisplayName = "isn't a symbol")]
    public void Failure()
    {
        const string literal = "not a close brace";

        Lexer lexer = new(literal);
        Assert.False(Ronin.Token.Symbol.IsSymbol(lexer));
        var lexed = Ronin.Token.Symbol.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "wrong way")]
    public void NotReturns()
    {
        const string literal = "=<";

        Lexer lexer = new(literal);
        Assert.False(Ronin.Token.Symbol.IsSymbol(lexer));
        var lexed = Ronin.Token.Symbol.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "wrong arrow")]
    public void NotReturns2()
    {
        const string literal = "->";

        Lexer lexer = new(literal);
        Assert.False(Ronin.Token.Symbol.IsSymbol(lexer));
        var lexed = Ronin.Token.Symbol.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "no data")]
    public void Empty()
    {
        Lexer lexer = new(string.Empty);
        Assert.False(Ronin.Token.Symbol.IsSymbol(lexer));
        var lexed = Ronin.Token.Symbol.Lex(lexer);

        Assert.Null(lexed);
    }
}
