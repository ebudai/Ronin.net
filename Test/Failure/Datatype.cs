using Ronin.Compiler;

namespace Failure;

public class Datatype
{
    [Fact(DisplayName = "doesn't start with 'datatype'")]
    public void Failure()
    {
        const string literal = "not a datatype";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Modifiers.Datatype.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "no data")]
    public void Empty()
    {
        Lexer lexer = new() { Sourcecode = string.Empty.ToArray() };
        var lexed = Ronin.Tokens.Modifiers.Datatype.Lex(lexer);

        Assert.Null(lexed);
    }
}
