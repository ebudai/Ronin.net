using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Ordinal : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // [test]

        List<Token> tokens = new()
        {
            StartOrdinal(),
            Word("test"),
            EndOrdinal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var ordinal = Ronin.Grammar.Compound.Ordinal.Parse(ref parser);

        Assert.Single(ordinal?.Values);
        var reference = ordinal.Values[0] as Ronin.Grammar.Reference;
        Assert.Single(reference?.Components);
        Ronin.Grammar.Words name = reference.Components[0];
        Assert.Equal(1, name?.Source.Length);
    }

    [Fact(DisplayName = "multidimensional")]
    public void Multidimensional()
    {
        // [test, stuff]

        List<Token> tokens = new()
        {
            StartOrdinal(),
            Word("test"),
            Separator(),
            Word("stuff"),
            EndOrdinal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var ordinal = Ronin.Grammar.Compound.Ordinal.Parse(ref parser);

        Assert.Equal(2, ordinal?.Values?.Count);

        {            
            var test = ordinal.Values[0] as Ronin.Grammar.Reference;
            Assert.Single(test?.Components);
            Ronin.Grammar.Words name = test.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            var stuff = ordinal.Values[1] as Ronin.Grammar.Reference;
            Assert.Single(stuff?.Components);
            Ronin.Grammar.Words name = stuff.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }
    }

    [Fact(DisplayName = "empty parenthesis")]
    public void Empty()
    {
        // []

        List<Token> tokens = new()
        {
            StartOrdinal(),
            EndOrdinal(),
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

        List<Token> tokens = new()
        {
            StartOrdinal(),
            Number(1),
            Separator(),
            Number(2),
            Separator(),
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
            Ronin.Grammar.Words name = reference.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }        
    }
}