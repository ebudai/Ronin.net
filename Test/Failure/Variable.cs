using Ronin.Compiler;

namespace Failure;

public class Variable
{
    [Fact(DisplayName = "doesn't start with 'var'")]
    public void Failure()
    {
        const string literal = "not a variable";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Modifiers.Variable.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "no data")]
    public void Empty()
    {
        Lexer lexer = new() { Sourcecode = string.Empty.ToArray() };
        var lexed = Ronin.Tokens.Modifiers.Variable.Lex(lexer);

        Assert.Null(lexed);
    }
}
