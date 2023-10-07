using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

using Function = Ronin.Grammar.Function;

namespace Failure;

/*[Trait(nameof(Analyzer), nameof(Function.Declaration))]
public class Equality : AnalysisTests
{
    [Fact(DisplayName = nameof(Data))]
    public void Data()
    {
        Datum.Declaration datum = new();
        int x = default;

        Assert.False(datum.Equals(x));
    }

    [Fact(DisplayName = nameof(Token))]
    public void Token()
    {
        Returns symbol = new();
        int x = default;

        Assert.False(symbol.Equals(x));
    }

    [Fact(DisplayName = nameof(Syntax))]
    public void Syntax()
    {
        Comparison assignment = new();
        int x = default;

        Assert.False(assignment.Equals(x));
    }

    [Fact(DisplayName = nameof(Identifier))]
    public void Identifiers()
    {
        Identifier name = Words("x");
        int x = default;

        Assert.False(name.Components[0].Equals(x));
    }
}
*/