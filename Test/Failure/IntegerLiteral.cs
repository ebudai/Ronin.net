using Ronin.Compiler;
using Ronin.Tokens.Literals;

namespace Failure;

public class IntegerLiteral
{
    [Fact(DisplayName = "doesn't start with a number")]
    public void DoesntStartWithANumber()
    {
        const string literal = "g98723";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Literal.Integer.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "contains invalid chars")]
    public void Invalid()
    {
        const string literal = "92v5";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Literal.Integer.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Lexer lexer = new() { Sourcecode = string.Empty.ToArray() };
        var lexed = Literal.Integer.Lex(lexer);

        Assert.Null(lexed);
    }
}
