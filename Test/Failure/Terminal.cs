using Ronin.Compiler;

namespace Failure;

public class Terminal
{
    [Fact(DisplayName = "isn't ;")]
    public void Failure()
    {
        const string literal = "not a terminal";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Symbols.Terminal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "no data")]
    public void Empty()
    {
        Lexer lexer = new() { Sourcecode = string.Empty.ToArray() };
        var lexed = Ronin.Tokens.Symbols.Terminal.Lex(lexer);

        Assert.Null(lexed);
    }
}
