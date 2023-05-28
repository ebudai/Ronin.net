using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Lexer", null)]
public class Symbol
{
    [Fact(DisplayName = "wrong arrow")]
    public void NotReturns2()
    {
        const string literal = "->";

        Lexer lexer = new(literal);
        var lexed = Ronin.Lexicon.Symbol.Lex(ref lexer);

        Assert.IsNotType<Returns>(lexed);
    }
}
