using Ronin.Compiler;
using Range = Ronin.Lexicon.Symbols.Range;

namespace Unit;

[Trait("Lexer", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class range
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string dots = "..";

        Lexer lexer = new(dots);
        var range = Range.Lex(ref lexer);

        Assert.Equal(dots.ToArray(), range?.sourcecode.ToArray());
    }
}
