using Ronin.Compiler;

namespace Failure;

public class Constant
{
    [Fact(DisplayName = "doesn't start with 'constant'")]
    public void Failure()
    {
        const string literal = "not constant";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Modifiers.Constant.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "no data")]
    public void Empty()
    {
        Lexer lexer = new() { Sourcecode = string.Empty.ToArray() };
        var lexed = Ronin.Tokens.Modifiers.Constant.Lex(lexer);

        Assert.Null(lexed);
    }
}
