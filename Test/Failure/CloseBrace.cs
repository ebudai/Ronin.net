using Ronin.Compiler;

namespace Failure;

public class CloseBrace
{
    [Fact(DisplayName = "isn't }")]
    public void Failure()
    {
        const string literal = "not a close brace";

        Lexer lexer = new(literal);
        var lexed = Ronin.Tokens.Symbols.CloseBrace.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "no data")]
    public void Empty()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Ronin.Tokens.Symbols.CloseBrace.Lex(lexer);

        Assert.Null(lexed);
    }
}
