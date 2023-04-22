using Ronin.Compiler;
using Ronin.Lexicon.Punctuation;

namespace Failure;

[Trait("Lexer", null)]
public class Symbol
{
    [Fact(DisplayName = "isn't a symbol")]
    public void Failure()
    {
        const string literal = "not a close brace";

        Lexer lexer = new(literal);
        Assert.False(Ronin.Lexicon.Symbol.IsSymbol(ref lexer));
        var lexed = Ronin.Lexicon.Symbol.Lex(ref lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "wrong arrow")]
    public void NotReturns2()
    {
        const string literal = "->";

        Lexer lexer = new(literal);
        var lexed = Ronin.Lexicon.Symbol.Lex(ref lexer);

        Assert.IsNotType<Returns>(lexed);
    }

    [Fact(DisplayName = "no data")]
    public void Empty()
    {
        Lexer lexer = new(string.Empty);
        Assert.False(Ronin.Lexicon.Symbol.IsSymbol(ref lexer));
        var lexed = Ronin.Lexicon.Symbol.Lex(ref lexer);

        Assert.Null(lexed);
    }
}
