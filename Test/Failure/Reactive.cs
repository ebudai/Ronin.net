using Ronin.Compiler;

namespace Failure;

public class Reactive
{
    [Fact(DisplayName = "doesn't start with 'reactive'")]
    public void Failure()
    {
        const string literal = "not reactive";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Modifiers.Reactive.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "no data")]
    public void Empty()
    {
        Lexer lexer = new() { Sourcecode = string.Empty.ToArray() };
        var lexed = Ronin.Tokens.Modifiers.Reactive.Lex(lexer);

        Assert.Null(lexed);
    }
}
