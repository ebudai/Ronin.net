using Ronin.Compiler;
using Ronin.Lexicon;

namespace Failure;

[Trait("Parser", null)]
public class Parameters
{
    [Fact(DisplayName = "does not start with (")]
    public void NotParameters()
    {
        // not parameters;

        Token[] tokens = 
        {
            new Word(),
            new Word(),
            new TerminalSymbol(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var parameters = Ronin.Grammar.Aggregates.Parameters.Parse(ref parser);

        Assert.Null(parameters);
    }

    [Fact(DisplayName = "blank")]
    public void Blank()
    {
        Token[] tokens = { Sentinel.Instance };
        Parser parser = new(tokens);
        var parameters = Ronin.Grammar.Aggregates.Parameters.Parse(ref parser);

        Assert.Null(parameters);
    }

    [Fact(DisplayName = "bad component")]
    public void BadComponent()
    {
        // (test => money, [thing;stuff])

        Token[] tokens = 
        {
            new OpenParenthesisSymbol(),
            new Word(),
            new ReturnsSymbol(),
            new Word(),
            new SeparatorSymbol(),
            new OpenSquareBracketSymbol(),
            new Word(),
            new TerminalSymbol(),
            new Word(),
            new CloseSquareBracketSymbol(),
            new CloseParenthesisSymbol(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var parameters = Ronin.Grammar.Aggregates.Parameters.Parse(ref parser);

        Assert.Null(parameters);
    }

    [Fact(DisplayName = "terminated incorrectly")]
    public void TerminatedWrong()
    {
        // (test => text;)

        Token[] tokens = 
        {
            new Word(),
            new ReturnsSymbol(),
            new Word(),
            new TerminalSymbol(),
            new CloseParenthesisSymbol(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var parameters = Ronin.Grammar.Aggregates.Parameters.Parse(ref parser);

        Assert.Null(parameters);
    }
}
