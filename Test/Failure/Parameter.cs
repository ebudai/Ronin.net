using Ronin.Compiler;
using Ronin.Grammar.Compound;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;
using Test;

namespace Failure;

[Trait("Parser", null)]
public class Parameter : ParsingTests
{
    [Fact(DisplayName = "does not start with (")]
    public void NotParameters()
    {
        // not parameters;

        List<Token> tokens = new()
        {
            Word("not"),
            Word("parameters"),
            Terminal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var parameters = Parameters.Parse(ref parser);

        Assert.Null(parameters);
    }

    [Fact(DisplayName = "blank")]
    public void Blank()
    {
        List<Token> tokens = new() { Sentinel.Instance };
        Parser parser = new(tokens);
        var parameters = Parameters.Parse(ref parser);

        Assert.Null(parameters);
    }

    [Fact(DisplayName = "bad component")]
    public void BadComponent()
    {
        // (test => money, [thing;stuff])

        List<Token> tokens = new()
        {
            StartValues(),
            Word("test"),
            Returns(),
            Word("money"),
            Separator(),
            StartIndexer(),
            Word("thing"),
            Terminal(),
            Word("stuff"),
            EndIndexer(),
            EndValues(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var parameters = Parameters.Parse(ref parser);

        Assert.Null(parameters);
    }

    [Fact(DisplayName = "terminated incorrectly")]
    public void TerminatedWrong()
    {
        // (test => text;)

        List<Token> tokens = new()
        {
            Word("test"),
            Returns(),
            Word("text"),
            Terminal(),
            EndValues(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var parameters = Parameters.Parse(ref parser);

        Assert.Null(parameters);
    }
}
