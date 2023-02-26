using Ronin.Compiler;
using Ronin.Lexicon;

namespace Failure;

[Trait("Lexer", null)]
public class Url
{
    [Fact(DisplayName = "unterminated url")]
    public void Unterminated()
    {
        const string literal = "abc://";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "too short")]
    public void TooShort()
    {
        const string literal = "a://";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad url scheme")]
    public void BadScheme()
    {
        const string literal = "123things://stuff.com";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "no ://")]
    public void MissingSymbols()
    {
        const string literal = "notAUrlLiteral";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }
}
