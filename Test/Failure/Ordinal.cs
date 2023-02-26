using Ronin.Compiler;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;

namespace Failure;

[Trait("Parser", null)]
public class Ordinal
{
    [Fact(DisplayName = "does not start with [")]
    public void NotAnOrdinal()
    {
        // not an ordinal;

        Token[] tokens =
        {
            new Word(),
            new Word(),
            new Word(),
            new TerminalSymbol()
        };
        
        Parser parser = new(tokens);
        var ordinal = Ronin.Grammar.Aggregates.Ordinal.Parse(ref parser);

        Assert.Null(ordinal);
    }

    [Fact(DisplayName = "blank")]
    public void Blank()
    {
        Token[] tokens = { Sentinel.Instance };
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Aggregates.Ordinal.Parse(ref parser);

        Assert.Null(arguments);
    }

    [Fact(DisplayName = "bad component")]
    public void BadComponent()
    {
        // [test, (thing;stuff)]

        Token[] tokens =
        {
            new OpenSquareBracketSymbol(),
            new Word(),
            new SeparatorSymbol(),
            new Word(),
            new TerminalSymbol(),
            new Word(),
            new CloseParenthesisSymbol(),
            new CloseSquareBracketSymbol()
        };
        
        Parser parser = new(tokens);
        var ordinal = Ronin.Grammar.Aggregates.Ordinal.Parse(ref parser);

        Assert.Null(ordinal);
    }

    [Fact(DisplayName = "terminated incorrectly")]
    public void TerminatedWrong()
    {
        // [test;]

        Token[] tokens =
        {
            new OpenSquareBracketSymbol(),
            new Word(),
            new TerminalSymbol(),
            new CloseSquareBracketSymbol()
        };
        
        Parser parser = new(tokens);
        var ordinal = Ronin.Grammar.Aggregates.Ordinal.Parse(ref parser);

        Assert.Null(ordinal);
    }
}
