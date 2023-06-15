using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Unit;

[Trait("Lexer", null)]
public class RangeSymbol
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string dots = "..";

        Lexer lexer = new(dots);
        var range = Interval.Lex(ref lexer);

        Assert.Equal(dots.ToArray(), range?.Memory.ToArray());
    }
}
