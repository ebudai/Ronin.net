using Ronin.Compiler;

namespace Failure;

public class Function
{
    [Fact(DisplayName = "doesn't start with 'function'")]
    public void Failure()
    {
        const string literal = "not a function";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Modifiers.Function.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "no data")]
    public void Empty()
    {
        Lexer lexer = new() { Sourcecode = string.Empty.ToArray() };
        var lexed = Ronin.Tokens.Modifiers.Function.Lex(lexer);

        Assert.Null(lexed);
    }
}
