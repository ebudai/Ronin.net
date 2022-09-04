using Ronin.Compiler;

namespace Failure;

public class Reactive
{
    [Fact(DisplayName = "doesn't start with 'reactive'")]
    public void Failure()
    {
        const string literal = "not reactive";

        Lexer lexer = new(literal);
        var lexed = Ronin.Tokens.Modifiers.Reactive.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "no data")]
    public void Empty()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Ronin.Tokens.Modifiers.Reactive.Lex(lexer);

        Assert.Null(lexed);
    }
}
