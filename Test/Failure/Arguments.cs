using Ronin.Compiler;
using Ronin.Lexicon;

namespace Failure;

[Trait("Parser", null)]
public class Arguments
{
    [Fact(DisplayName = "does not start with (")]
    public void NotAnArguments()
    {
        // not an object;

        Token[] tokens = 
        {
            new Word(),
            new Word(),
            new Word(),
            new TerminalSymbol()
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Aggregates.Arguments.Parse(ref parser);

        Assert.Null(arguments);
    }

    [Fact(DisplayName = "blank")]
    public void Blank()
    {
        Token[] tokens = { Sentinel.Instance };
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Aggregates.Arguments.Parse(ref parser);

        Assert.Null(arguments);
    }

    [Fact(DisplayName = "bad separator")]
    public void BadSeparator()
    {
        // (test, (thing;stuff))

        Token[] tokens =
        {
            new OpenParenthesisSymbol(),
            new Word(),
            new SeparatorSymbol(),
            new OpenParenthesisSymbol(),
            new Word(),
            new TerminalSymbol(),
            new Word(),
            new CloseParenthesisSymbol(),
            new CloseParenthesisSymbol()
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Aggregates.Arguments.Parse(ref parser);
        
        Assert.Null(arguments);
    }

    [Fact(DisplayName = "terminated incorrectly")]
    public void TerminatedWrong()
    {
        // (test;)

        Token[] tokens =
        {
            new OpenParenthesisSymbol(),
            new Word(),
            new TerminalSymbol(),
            new CloseParenthesisSymbol()
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Aggregates.Arguments.Parse(ref parser);
        
        Assert.Null(arguments);
    }
}
