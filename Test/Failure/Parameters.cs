using Ronin;
using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

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
            new Word { sourcecode = "parameters".AsMemory() },
            new Terminal { sourcecode = new[] { Terminal.symbol } },
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
            new StartValues { sourcecode = new[] { StartValues.symbol } },
            new Word { sourcecode = "test".AsMemory() },
            new Returns { sourcecode = Returns.symbol.AsMemory() },
            new Word { sourcecode = "money".AsMemory() },
            new Separator { sourcecode = new[] { Separator.symbol } },
            new StartOrdinal { sourcecode = new[] { StartOrdinal.symbol } },
            new Word { sourcecode = "thing".AsMemory() },
            new Terminal { sourcecode = new[] { Terminal.symbol } },
            new Word { sourcecode = "stuff".AsMemory() },
            new EndOrdinal { sourcecode = new[] { EndOrdinal.symbol } },
            new EndValues { sourcecode = new[] { EndValues.symbol } },
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
            new Terminal { sourcecode = new[] { Terminal.symbol } },
            new EndValues { sourcecode = new[] { EndValues.symbol } },
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var parameters = Ronin.Grammar.Compound.Parameters.Parse(ref parser);

        Assert.Null(parameters);
    }
}
