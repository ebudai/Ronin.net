using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keyword;
using Ronin.Lexicon.Punctuation;

namespace Unit;

[Trait("Parser", null)]
public class Scope
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
            new NumberLiteral(),
            new Terminal(),
            new CloseBrace(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var scope = Ronin.Grammar.Compound.Scope.Parse(ref parser);

        Assert.Single(scope?.Values);

        Ronin.Grammar.Datum datum = scope.Values[0];

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Equal(1, datum.Name?.Source.Length);

        Assert.Null(datum.Is);

        Ronin.Grammar.Literal scalar = datum.Initializer;
        Assert.Equal(1, scalar?.Source.Length);
    }
}