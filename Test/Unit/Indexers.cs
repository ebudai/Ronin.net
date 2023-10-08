using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;
using Literal = Ronin.Grammar.Literal;

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
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var indexer = Ronin.Grammar.Index.Parse(ref parser);

        Assert.Single(indexer);
        var member = indexer[0] as Member.Unresolved;
        Assert.Single(member?.Reference?.Components);
        var name = member.Reference.Components[0].AsT0;
        Assert.Single(name?.Tokens.ToArray());
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
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var indexer = Ronin.Grammar.Index.Parse(ref parser);

        Assert.Equal(2, indexer?.Count);

        {
            var member = indexer[0] as Member.Unresolved;
            Assert.Single(member?.Reference);
            Name name = member.Reference.Components[0].AsT0;
            Assert.Equal(1, name?.Tokens.Length);
        }

        {
            var member = indexer[1] as Member.Unresolved;
            Assert.Single(member?.Reference);
            var name = member.Reference.Components[0].AsT0;
            Assert.Single(name?.Tokens.ToArray());
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
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var indexer = Ronin.Grammar.Index.Parse(ref parser);

        Assert.Null(indexer);
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
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var arguments = Ronin.Grammar.Index.Parse(ref parser);

        Assert.Equal(3, arguments?.Count);

        {
            var scalar = arguments[0] as Literal;
            Assert.Single(scalar?.Tokens.ToArray());
        }

        {
            var scalar = arguments[1] as Literal;
            Assert.Single(scalar?.Tokens.ToArray());
        }

        {
            var member = arguments[2] as Member.Unresolved;
            Assert.Single(member?.Reference?.Components);
            var name = member.Reference.Components[0].AsT0;
            Assert.Single(name?.Tokens.ToArray());
        }        
    }
}