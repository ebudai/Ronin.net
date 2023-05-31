using Ronin.Compiler;

namespace Unit;

[Trait("Lexer", null)]
public class RangeSymbol
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string dots = "..";

        Lexer lexer = new(dots);
        var range = Ronin.Lexicon.Symbols.Range.Lex(ref lexer);

        Assert.Equal(dots.ToArray(), range?.sourcecode.ToArray());
    }
}
