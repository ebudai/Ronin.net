using Ronin.Compiler;

namespace Failure;

public class Separator
{
    [Fact(DisplayName = "isn't ,")]
    public void Failure()
    {
        const string literal = "not a separator";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Symbols.Separator.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "no data")]
    public void Empty()
    {
        Lexer lexer = new() { Sourcecode = string.Empty.ToArray() };
        var lexed = Ronin.Tokens.Symbols.Separator.Lex(lexer);

        Assert.Null(lexed);
    }
}
