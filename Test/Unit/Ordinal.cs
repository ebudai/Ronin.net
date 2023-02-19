using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class ordinal
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
        var ordinal = Ordinal.Parse(ref parser);

        Assert.Single(ordinal?.Values);
        Reference reference = ordinal.Values[0];
        Assert.Single(reference?.Components);
        Name name = reference.Components[0];
        Assert.Single(name?.Words);
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
        var ordinal = Ordinal.Parse(ref parser);

        Assert.Equal(2, ordinal?.Values?.Count);

        {            
            Reference test = ordinal.Values[0];
            Assert.Single(test?.Components);
            Name name = test.Components[0];
            Assert.Single(name?.Words);
        }

        {
            Reference stuff = ordinal.Values[1];
            Assert.Single(stuff?.Components);
            Name name = stuff.Components[0];
            Assert.Single(name?.Words);
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
        var ordinal = Ordinal.Parse(ref parser);

        Assert.Empty(ordinal?.Values);
    }

    [Fact(DisplayName = "multidimensional named")]
    public void MultidimensionalNamed()
    {
        // [1, 2, thing]

        Token[] tokens = 
        {
            new OpenSquareBracket(),
            new Number(),
            new Separator(),
            new Number(),
            new Separator(),
            new Word(),
            new CloseSquareBracket(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var arguments = Ordinal.Parse(ref parser);

        Assert.Equal(3, arguments?.Values?.Count);

        {
            Scalar scalar = arguments.Values[0];
            Assert.Single(scalar?.Literals);
        }

        {
            Scalar scalar = arguments.Values[1];
            Assert.Single(scalar?.Literals);
        }

        {
            Reference reference = arguments.Values[2];
            Assert.Single(reference?.Components);
            Name name = reference.Components[0];
            Assert.Single(name?.Words);
        }        
    }
}