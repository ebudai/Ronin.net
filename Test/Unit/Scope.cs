using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class scope
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // { var test = 56; }

        Token[] tokens = 
        {
            new OpenBrace(),
            new Variable(),
            new Word(),
            new Assign(),
            new Number(),
            new Terminal(),
            new CloseBrace(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var scope = Scope.Parse(ref parser);

        Assert.Single(scope?.Values);

        Datum datum = scope.Values[0];

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Single(datum.Name?.Source);

        Assert.Null(datum.Is);

        Scalar scalar = datum.Initializer;
        Assert.Single(scalar?.Source);
    }
}
