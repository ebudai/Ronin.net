using Ronin.Compiler;

namespace Failure;

public class OpenBrace
{
    [Fact(DisplayName = "isn't {")]
    public void Failure()
    {
        const string literal = "not an open brace";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Symbols.OpenBrace.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "no data")]
    public void Empty()
    {
        Lexer lexer = new() { Sourcecode = string.Empty.ToArray() };
        var lexed = Ronin.Tokens.Symbols.OpenBrace.Lex(lexer);

        Assert.Null(lexed);
    }
}
