using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Lexer", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class symbol
{
    [Fact(DisplayName = "isn't a symbol")]
    public void Failure()
    {
        const string literal = "not a close brace";

        Lexer lexer = new(literal);
        Assert.False(Symbol.IsSymbol(lexer));
        var lexed = Symbol.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "wrong arrow")]
    public void NotReturns2()
    {
        const string literal = "->";

        Lexer lexer = new(literal);
        var lexed = Symbol.Lex(lexer);

        Assert.IsNotType<Returns>(lexed);
    }

    [Fact(DisplayName = "no data")]
    public void Empty()
    {
        Lexer lexer = new(string.Empty);
        Assert.False(Symbol.IsSymbol(lexer));
        var lexed = Symbol.Lex(lexer);

        Assert.Null(lexed);
    }
}
