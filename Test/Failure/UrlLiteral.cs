using Ronin.Compiler;

using Literal = Ronin.Tokens.Literals.UrlLiteral;

namespace Failure;

public class UrlLiteral
{
    [Fact(DisplayName = "unterminated url")]
    public void Unterminated()
    {
        const string literal = "a://";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad url scheme")]
    public void BadScheme()
    {
        const string literal = "123things://stuff.com";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "no ://")]
    public void MissingSymbols()
    {
        const string literal = "notAUrlLiteral";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }


}
