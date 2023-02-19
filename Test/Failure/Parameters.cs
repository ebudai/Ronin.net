using Ronin.Compiler;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class parameters
{
    [Fact(DisplayName = "does not start with (")]
    public void NotParameters()
    {
        // not parameters;

        Token[] tokens = 
        {
            new Word(),
            new Word(),
            new Terminal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var parameters = Parameters.Parse(ref parser);

        Assert.Null(parameters);
    }

    [Fact(DisplayName = "blank")]
    public void Blank()
    {
        Token[] tokens = { Sentinel.Instance };
        Parser parser = new(tokens);
        var parameters = Parameters.Parse(ref parser);

        Assert.Null(parameters);
    }

    [Fact(DisplayName = "bad component")]
    public void BadComponent()
    {
        // (test => money, [thing;stuff])

        Token[] tokens = 
        {
            new OpenParenthesis(),
            new Word(),
            new Returns(),
            new Word(),
            new Separator(),
            new OpenSquareBracket(),
            new Word(),
            new Terminal(),
            new Word(),
            new CloseSquareBracket(),
            new CloseParenthesis(),
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

        Token[] tokens = 
        {
            new Word(),
            new Returns(),
            new Word(),
            new Terminal(),
            new CloseParenthesis(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var parameters = Parameters.Parse(ref parser);

        Assert.Null(parameters);
    }
}
