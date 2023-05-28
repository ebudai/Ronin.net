using Ronin;
using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

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
            new StartOrdinal(),
            new Word(),
            new EndOrdinal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var ordinal = Ronin.Grammar.Compound.Ordinal.Parse(ref parser);

        Assert.Single(ordinal?.Values);
        var reference = ordinal.Values[0] as Ronin.Grammar.Reference;
        Assert.Single(reference?.Components);
        Ronin.Grammar.Name name = reference.Components[0];
        Assert.Equal(1, name?.Source.Length);
    }

    [Fact(DisplayName = "multidimensional")]
    public void Multidimensional()
    {
        // [test, stuff]

        Token[] tokens = 
        {
            new StartOrdinal(),
            new Word(),
            new Separator(),
            new Word(),
            new EndOrdinal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var ordinal = Ronin.Grammar.Compound.Ordinal.Parse(ref parser);

        Assert.Equal(2, ordinal?.Values?.Count);

        {            
            var test = ordinal.Values[0] as Ronin.Grammar.Reference;
            Assert.Single(test?.Components);
            Ronin.Grammar.Name name = test.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            var stuff = ordinal.Values[1] as Ronin.Grammar.Reference;
            Assert.Single(stuff?.Components);
            Ronin.Grammar.Name name = stuff.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }
    }

    [Fact(DisplayName = "empty parenthesis")]
    public void Empty()
    {
        // []

        Token[] tokens = 
        {
            new StartOrdinal(),
            new EndOrdinal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var ordinal = Ronin.Grammar.Compound.Ordinal.Parse(ref parser);

        Assert.Empty(ordinal?.Values);
    }

    [Fact(DisplayName = "multidimensional named")]
    public void MultidimensionalNamed()
    {
        // [1, 2, thing]

        Token[] tokens = 
        {
            new StartOrdinal(),
            new NumberLiteral(),
            new Separator(),
            new NumberLiteral(),
            new Separator(),
            new Word(),
            new EndOrdinal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Ordinal.Parse(ref parser);

        Assert.Equal(3, arguments?.Values?.Count);

        {
            var scalar = arguments.Values[0] as Ronin.Grammar.Literal;
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            var scalar = arguments.Values[1] as Ronin.Grammar.Literal;
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            var reference = arguments.Values[2] as Ronin.Grammar.Reference;
            Assert.Single(reference?.Components);
            Ronin.Grammar.Name name = reference.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }        
    }
}