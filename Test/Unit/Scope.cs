using Ronin.Compiler;
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

        var datum = scope.Values[0] as Ronin.Grammar.DatumDeclaration;

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Single(datum.Name?.Components);

        Assert.Null(datum.Is);

        var scalar = datum.Initializer as Ronin.Grammar.Literal;
        Assert.Equal(1, scalar?.Source.Length);
    }
}