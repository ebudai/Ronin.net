using Ronin;
using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Punctuation;

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
            new Word { sourcecode = "not".AsMemory() },
            new Word{ sourcecode = "parameters".AsMemory() },
            new Terminal { sourcecode = Terminal.symbol.AsMemory() },
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var parameters = Ronin.Grammar.Compound.Parameters.Parse(ref parser);

        Assert.Null(parameters);
    }

    [Fact(DisplayName = "blank")]
    public void Blank()
    {
        Token[] tokens = { Sentinel.Instance };
        Parser parser = new(tokens);
        var parameters = Ronin.Grammar.Compound.Parameters.Parse(ref parser);

        Assert.Null(parameters);
    }

    [Fact(DisplayName = "bad component")]
    public void BadComponent()
    {
        // (test => money, [thing;stuff])

        Token[] tokens = 
        {
            new OpenParenthesis { sourcecode = OpenParenthesis.symbol.AsMemory() },
            new Word { sourcecode = "test".AsMemory() },
            new Returns { sourcecode = Returns.symbol.AsMemory() },
            new Word { sourcecode = "money".AsMemory() },
            new Separator { sourcecode = Separator.symbol.AsMemory() },
            new OpenSquareBracket { sourcecode = OpenSquareBracket.symbol.AsMemory() },
            new Word { sourcecode = "thing".AsMemory() },
            new Terminal { sourcecode = Terminal.symbol.AsMemory() },
            new Word { sourcecode = "stuff".AsMemory() },
            new CloseSquareBracket { sourcecode = CloseSquareBracket.symbol.AsMemory() },
            new CloseParenthesis { sourcecode = CloseParenthesis.symbol.AsMemory() },
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var parameters = Ronin.Grammar.Compound.Parameters.Parse(ref parser);

        Assert.Null(parameters);
    }

    [Fact(DisplayName = "terminated incorrectly")]
    public void TerminatedWrong()
    {
        // (test => text;)

        Token[] tokens = 
        {
            new Word { sourcecode = "test".AsMemory() },
            new Returns { sourcecode = Returns.symbol.AsMemory() },
            new Word{ sourcecode = "text".AsMemory() },
            new Terminal { sourcecode = Terminal.symbol.AsMemory() },
            new CloseParenthesis { sourcecode = CloseParenthesis.symbol.AsMemory() },
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var parameters = Ronin.Grammar.Compound.Parameters.Parse(ref parser);

        Assert.Null(parameters);
    }
}
