using Ronin.Compiler;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class ordinal
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
            new Terminal()
        };
        
        Parser parser = new(tokens);
        var ordinal = Ordinal.Parse(ref parser);

        Assert.Null(ordinal);
    }

    [Fact(DisplayName = "blank")]
    public void Blank()
    {
        Token[] tokens = { Sentinel.Instance };
        Parser parser = new(tokens);
        var arguments = Ordinal.Parse(ref parser);

        Assert.Null(arguments);
    }

    [Fact(DisplayName = "bad component")]
    public void BadComponent()
    {
        // [test, (thing;stuff)]

        Token[] tokens =
        {
            new OpenSquareBracket(),
            new Word(),
            new Separator(),
            new Word(),
            new Terminal(),
            new Word(),
            new CloseParenthesis(),
            new CloseSquareBracket()
        };
        
        Parser parser = new(tokens);
        var ordinal = Ordinal.Parse(ref parser);

        Assert.Null(ordinal);
    }

    [Fact(DisplayName = "terminated incorrectly")]
    public void TerminatedWrong()
    {
        // [test;]

        Token[] tokens =
        {
            new OpenSquareBracket(),
            new Word(),
            new Terminal(),
            new CloseSquareBracket()
        };
        
        Parser parser = new(tokens);
        var ordinal = Ordinal.Parse(ref parser);

        Assert.Null(ordinal);
    }
}
