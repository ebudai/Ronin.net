using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Failure;

[Trait("Parser", null)]
public class Indexers : ParsingTests
{
    [Fact(DisplayName = "does not start with [")]
    public void NotAnIndexer()
    {
        // not an indexer;

        List<Token> tokens = new()
        {
            Word("not"),
            Word("an"),
            Word("indexer"),
            Terminal()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var indexer = Ronin.Grammar.Index.Parse(ref parser);

        Assert.Null(indexer);
    }

    [Fact(DisplayName = "blank")]
    public void Blank()
    {
        List<Token> tokens = new() { Sentinel.Instance };
        Parser parser = new(tokens.AsLinkedList());
        var arguments = Ronin.Grammar.Index.Parse(ref parser);

        Assert.Null(arguments);
    }

    [Fact(DisplayName = "bad component")]
    public void BadComponent()
    {
        // [test, (thing;stuff)]

        List<Token> tokens = new()
        {
            StartIndexer(),
            Word("test"),
            Separator(),
            Word("thing"),
            Terminal(),
            Word("stuff"),
            EndValues(),
            EndIndexer(),
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var indexer = Ronin.Grammar.Index.Parse(ref parser);

        Assert.Null(indexer);
    }

    [Fact(DisplayName = "terminated incorrectly")]
    public void TerminatedWrong()
    {
        // [test;]

        List<Token> tokens = new()
        {
            StartIndexer(),
            Word("test"),
            Terminal(),
            EndIndexer(),
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var indexer = Ronin.Grammar.Index.Parse(ref parser);

        Assert.Null(indexer);
    }
}
