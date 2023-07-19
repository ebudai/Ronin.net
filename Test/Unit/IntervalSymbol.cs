using Ronin.Compiler;
using Ronin.Lexicon;

namespace Unit;

[Trait("Lexer", null)]
public class IntervalSymbol
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string dots = "..";

        Lexer lexer = new(dots);
        var interval = Symbol.Lex(ref lexer);

        Assert.Equal(dots.ToArray(), interval?.Memory.ToArray());
    }
}
