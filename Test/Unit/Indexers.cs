using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
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

        Assert.Single(indexer);
        var unresolved = indexer[0] as Reference.Unresolved;
        var member = unresolved.Member as Context.Member.Unresolved;
        Assert.Single(member?.Reference?.Components);
        Name name = member.Reference.Components[0];
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

        Assert.Equal(2, indexer?.Count);

        {            
            var test = indexer[0] as Reference.Unresolved;
            var member = test.Member as Context.Member.Unresolved;
            Assert.Single(member?.Reference?.Components);
            Name name = member.Reference.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            var stuff = indexer[1] as Reference.Unresolved;
            var member = stuff.Member as Context.Member.Unresolved;
            Assert.Single(member?.Reference?.Components);
            Name name = member.Reference.Components[0];
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

        Assert.Empty(indexer);
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

        Assert.Equal(3, arguments?.Count);

        {
            var scalar = arguments[0] as Inline;
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            var scalar = arguments[1] as Inline;
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            var unresolved = arguments[2] as Reference.Unresolved;
            var member = unresolved.Member as Context.Member.Unresolved;
            Assert.Single(member?.Reference?.Components);
            Name name = member.Reference.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }        
    }
}