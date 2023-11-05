using Ronin.Compiler;
using Ronin.Lexicon;

namespace Failure;

[Trait(nameof(Lexer), null)]
public class Intervals
{
    [Fact(DisplayName = "not a range")]
    public void NotRange()
    {
        const string literal = "notARange";

        Lexer lexer = new(literal);
        var lexed = Symbol.Special.Interval.Lex(ref lexer);

        Assert.IsNotType<Symbol.Special.Interval>(lexed);
    }
}
