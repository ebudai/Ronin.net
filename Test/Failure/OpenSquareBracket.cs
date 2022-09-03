using Ronin.Compiler;

namespace Failure;

public class OpenSquareBracket
{
    [Fact(DisplayName = "isn't [")]
    public void Failure()
    {
        const string literal = "not an open square bracket";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Symbols.OpenSquareBracket.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "no data")]
    public void Empty()
    {
        Lexer lexer = new() { Sourcecode = string.Empty.ToArray() };
        var lexed = Ronin.Tokens.Symbols.OpenSquareBracket.Lex(lexer);

        Assert.Null(lexed);
    }
}
