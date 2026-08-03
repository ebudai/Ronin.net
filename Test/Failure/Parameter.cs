using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Failure;

[Trait(nameof(Parser), null)]
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
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var parameters = Parameters.Parse(ref parser);

        Assert.Null(parameters);
    }

    [Fact(DisplayName = "blank")]
    public void Blank()
    {
        List<Token> tokens = new() { new Sentinel() };
        Parser parser = new(tokens.AsLinkedList());
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
            StartBracket(),
            Word("thing"),
            Terminal(),
            Word("stuff"),
            EndBracket(),
            EndValues(),
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
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
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var parameters = Parameters.Parse(ref parser);

        Assert.Null(parameters);
    }
}
