using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Compound;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Definitions : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // { var test = 56; }

        List<Token> tokens = new()
        {
            StartScope(),
            Keyword.Variable(),
            Word("test"),
            Assign(),
            Number(56),
            Terminal(),
            EndScope(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var scope = Definition.Parse(ref parser);

        Assert.Single(scope?.Values);

        var datum = scope.Values[0] as Datum.Declaration;

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Equal(1, datum.Name?.Source.Length);

        Assert.False(datum.Modifiers.Is<Compiled>());
        Assert.False(datum.Modifiers.Is<Shared>());
        Assert.False(datum.Modifiers.Is<Optional>());
        Assert.False(datum.Modifiers.Is<Persistent>());

        var scalar = datum.Initializer as Inline;
        Assert.Equal(1, scalar?.Source.Length);
    }
}