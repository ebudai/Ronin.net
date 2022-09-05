using Ronin.Compiler;

namespace Failure;

public class Symbol
{
    [Fact(DisplayName = "isn't a symbol")]
    public void Failure()
    {
        const string literal = "not a close brace";

        Lexer lexer = new(literal);
        var lexed = Ronin.Token.Symbol.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "no data")]
    public void Empty()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Ronin.Token.Symbol.Lex(lexer);

        Assert.Null(lexed);
    }

}
