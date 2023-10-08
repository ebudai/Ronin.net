using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Failure;

[Trait("Parser", null)]
public class Input : ParsingTests
{
    [Fact(DisplayName = "does not start with (")]
    public void NotAnArguments()
    {
        // not an object;

        List<Token> tokens = new()
        {
            Word("not"),
            Word("an"),
            Word("object"),
            Terminal(),
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var arguments = Inputs.Parse(ref parser);

        Assert.Null(arguments);
    }

    [Fact(DisplayName = "blank")]
    public void Blank()
    {
        List<Token> tokens = new() { new Sentinel() };
        Parser parser = new(tokens.AsLinkedList());
        var arguments = Inputs.Parse(ref parser);

        Assert.Null(arguments);
    }

    [Fact(DisplayName = "bad separator")]
    public void BadSeparator()
    {
        // (test, (thing;stuff))

        List<Token> tokens = new()
        {
            StartValues(),
            Word("test"),
            Separator(),
            StartValues(),
            Word("thing"),
            Terminal(),
            Word("stuff"),
            EndValues(),
            EndValues(),
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var arguments = Inputs.Parse(ref parser);
        
        Assert.Null(arguments);
    }

    [Fact(DisplayName = "terminated incorrectly")]
    public void TerminatedWrong()
    {
        // (test;)

        List<Token> tokens = new()
        {
            StartValues(),
            Word("test"),
            Terminal(),
            EndValues(),
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var arguments = Inputs.Parse(ref parser);
        
        Assert.Null(arguments);
    }
}
