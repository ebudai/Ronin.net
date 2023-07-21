using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Compound;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Indexers : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // [test]

        List<Token> tokens = new()
        {
            StartIndexer(),
            Word("test"),
            EndIndexer(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var indexer = Indexer.Parse(ref parser);

        Assert.Single(indexer?.Values);
        var unresolved = indexer.Values[0] as Value.Unresolved;
        Assert.Single(unresolved?.Reference?.Components);
        Name name = unresolved.Reference.Components[0];
        Assert.Equal(1, name?.Source.Length);
    }

    [Fact(DisplayName = "multidimensional")]
    public void Multidimensional()
    {
        // [test, stuff]

        List<Token> tokens = new()
        {
            StartIndexer(),
            Word("test"),
            Separator(),
            Word("stuff"),
            EndIndexer(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var indexer = Indexer.Parse(ref parser);

        Assert.Equal(2, indexer?.Values?.Count);

        {            
            var test = indexer.Values[0] as Value.Unresolved;
            Assert.Single(test?.Reference?.Components);
            Name name = test.Reference.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            var stuff = indexer.Values[1] as Value.Unresolved;
            Assert.Single(stuff?.Reference?.Components);
            Name name = stuff.Reference.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }
    }

    [Fact(DisplayName = "empty parenthesis")]
    public void Empty()
    {
        // []

        List<Token> tokens = new()
        {
            StartIndexer(),
            EndIndexer(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var indexer = Indexer.Parse(ref parser);

        Assert.Empty(indexer?.Values);
    }

    [Fact(DisplayName = "multidimensional named")]
    public void MultidimensionalNamed()
    {
        // [1, 2, thing]

        List<Token> tokens = new()
        {
            StartIndexer(),
            Number(1),
            Separator(),
            Number(2),
            Separator(),
            Word("thing"),
            EndIndexer(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var arguments = Indexer.Parse(ref parser);

        Assert.Equal(3, arguments?.Values?.Count);

        {
            var scalar = arguments.Values[0] as Inline;
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            var scalar = arguments.Values[1] as Inline;
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            var unresolved = arguments.Values[2] as Value.Unresolved;
            Assert.Single(unresolved?.Reference?.Components);
            Name name = unresolved.Reference.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }        
    }
}