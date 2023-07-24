using Ronin.Compiler;
using Ronin.Lexicon;

namespace Failure;

[Trait("Lexer", null)]
public class Symbols
{
    [Fact(DisplayName = "wrong arrow")]
    public void NotReturns2()
    {
        const string literal = "->";

        Lexer lexer = new(literal);
        var lexed = Symbol.Lex(ref lexer);

        Assert.IsNotType<Returns>(lexed);
    }

    [Fact(DisplayName = "empty")]
    public void Blank()
    {
        const string blank = "";

        Lexer lexer = new(blank);
        var lexed = Symbol.Lex(ref lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "not a symbol")]
    public void NotASymbol()
    {
        const string literal = "a";

        Lexer lexer = new(literal);
        var lexed = Symbol.Lex(ref lexer);

        Assert.Null(lexed);
    }
}
