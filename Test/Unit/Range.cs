using Ronin.Compiler;
using Ronin.Lexicon;

namespace Unit;

[Trait("Lexer", null)]
public class Range
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string dots = "..";

        Lexer lexer = new(dots);
        var range = RangeSymbol.Lex(ref lexer);

        Assert.Equal(dots.ToArray(), range?.sourcecode.ToArray());
    }
}
