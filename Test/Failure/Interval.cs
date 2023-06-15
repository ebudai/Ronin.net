using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Lexer", null)]
public class Intervals
{
    [Fact(DisplayName = "not a range")]
    public void NotRange()
    {
        const string literal = "notARange";

        Lexer lexer = new(literal);
        var lexed = Interval.Lex(ref lexer);

        Assert.IsNotType<Interval>(lexed);
    }
}
