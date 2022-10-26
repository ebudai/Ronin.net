using Ronin.Compiler;
using Ronin.Lexicon;

namespace Failure;

public class UrlLiteral
{
    [Fact(DisplayName = "unterminated url")]
    public void Unterminated()
    {
        const string literal = "abc://";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "too short")]
    public void TooShort()
    {
        const string literal = "a://";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad url scheme")]
    public void BadScheme()
    {
        const string literal = "123things://stuff.com";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "no ://")]
    public void MissingSymbols()
    {
        const string literal = "notAUrlLiteral";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.IsNotType<Literal>(lexed);
    }
}
