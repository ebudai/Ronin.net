using Ronin;
using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Punctuation;

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
            new OpenSquareBracket(),
            new Word(),
            new CloseSquareBracket(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var ordinal = Ronin.Grammar.Compound.Ordinal.Parse(ref parser);

        Assert.Single(ordinal?.Values);
        Ronin.Grammar.Reference reference = ordinal.Values[0];
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
            new OpenSquareBracket(),
            new Word(),
            new Separator(),
            new Word(),
            new CloseSquareBracket(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var ordinal = Ronin.Grammar.Compound.Ordinal.Parse(ref parser);

        Assert.Equal(2, ordinal?.Values?.Count);

        {            
            Ronin.Grammar.Reference test = ordinal.Values[0];
            Assert.Single(test?.Components);
            Ronin.Grammar.Name name = test.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            Ronin.Grammar.Reference stuff = ordinal.Values[1];
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
            new OpenSquareBracket(),
            new CloseSquareBracket(),
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
            new OpenSquareBracket(),
            new NumberLiteral(),
            new Separator(),
            new NumberLiteral(),
            new Separator(),
            new Word(),
            new CloseSquareBracket(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Ordinal.Parse(ref parser);

        Assert.Equal(3, arguments?.Values?.Count);

        {
            Ronin.Grammar.Literal scalar = arguments.Values[0];
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            Ronin.Grammar.Literal scalar = arguments.Values[1];
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            Ronin.Grammar.Reference reference = arguments.Values[2];
            Assert.Single(reference?.Components);
            Ronin.Grammar.Name name = reference.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }        
    }
}