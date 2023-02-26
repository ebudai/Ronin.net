using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

namespace Unit;

[Trait("Parser", null)]
public class Ordinal
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // [test]

        Token[] tokens =
        {
            new OpenSquareBracketSymbol(),
            new Word(),
            new CloseSquareBracketSymbol(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var ordinal = Ronin.Grammar.Aggregates.Ordinal.Parse(ref parser);

        Assert.Single(ordinal?.Values);
        Ronin.Grammar.Reference reference = ordinal.Values[0];
        Assert.Single(reference?.Components);
        Ronin.Grammar.Name name = reference.Components[0];
        Assert.Single(name?.Source);
    }

    [Fact(DisplayName = "multidimensional")]
    public void Multidimensional()
    {
        // [test, stuff]

        Token[] tokens = 
        {
            new OpenSquareBracketSymbol(),
            new Word(),
            new SeparatorSymbol(),
            new Word(),
            new CloseSquareBracketSymbol(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var ordinal = Ronin.Grammar.Aggregates.Ordinal.Parse(ref parser);

        Assert.Equal(2, ordinal?.Values?.Count);

        {            
            Ronin.Grammar.Reference test = ordinal.Values[0];
            Assert.Single(test?.Components);
            Ronin.Grammar.Name name = test.Components[0];
            Assert.Single(name?.Source);
        }

        {
            Ronin.Grammar.Reference stuff = ordinal.Values[1];
            Assert.Single(stuff?.Components);
            Ronin.Grammar.Name name = stuff.Components[0];
            Assert.Single(name?.Source);
        }
    }

    [Fact(DisplayName = "empty parenthesis")]
    public void Empty()
    {
        // []

        Token[] tokens = 
        {
            new OpenSquareBracketSymbol(),
            new CloseSquareBracketSymbol(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var ordinal = Ronin.Grammar.Aggregates.Ordinal.Parse(ref parser);

        Assert.Empty(ordinal?.Values);
    }

    [Fact(DisplayName = "multidimensional named")]
    public void MultidimensionalNamed()
    {
        // [1, 2, thing]

        Token[] tokens = 
        {
            new OpenSquareBracketSymbol(),
            new NumberLiteral(),
            new SeparatorSymbol(),
            new NumberLiteral(),
            new SeparatorSymbol(),
            new Word(),
            new CloseSquareBracketSymbol(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Aggregates.Ordinal.Parse(ref parser);

        Assert.Equal(3, arguments?.Values?.Count);

        {
            LiteralSyntax scalar = arguments.Values[0];
            Assert.Single(scalar?.Source);
        }

        {
            LiteralSyntax scalar = arguments.Values[1];
            Assert.Single(scalar?.Source);
        }

        {
            Ronin.Grammar.Reference reference = arguments.Values[2];
            Assert.Single(reference?.Components);
            Ronin.Grammar.Name name = reference.Components[0];
            Assert.Single(name?.Source);
        }        
    }
}