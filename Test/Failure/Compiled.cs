using Ronin.Compiler;

namespace Failure;

public class Compiled
{
    [Fact(DisplayName = "doesn't start with 'compiled'")]
    public void Failure()
    {
        const string literal = "not compiled";

        Lexer lexer = new(literal);
        var lexed = Ronin.Tokens.Modifiers.Compiled.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "no data")]
    public void Empty()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Ronin.Tokens.Modifiers.Compiled.Lex(lexer);

        Assert.Null(lexed);
    }
}
