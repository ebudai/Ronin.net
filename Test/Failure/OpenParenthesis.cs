using Ronin.Compiler;

namespace Failure;

public class OpenParenthesis
{
    [Fact(DisplayName = "isn't (")]
    public void Failure()
    {
        const string literal = "not an open parenthesis";

        Lexer lexer = new(literal);
        var lexed = Ronin.Tokens.Symbols.OpenParenthesis.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "no data")]
    public void Empty()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Ronin.Tokens.Symbols.OpenParenthesis.Lex(lexer);

        Assert.Null(lexed);
    }
}
