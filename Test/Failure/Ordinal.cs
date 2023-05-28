using Ronin;
using Ronin.Compiler;
using Ronin.Grammar.Compound;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

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
            new Word { sourcecode = "not".AsMemory() },
            new Word { sourcecode = "an".AsMemory() },
            new Word { sourcecode = "ordinal".AsMemory() },
            new Terminal { sourcecode = new[] { Terminal.symbol } }
        };
        
        Parser parser = new(tokens);
        var ordinal = Ronin.Grammar.Compound.Ordinal.Parse(ref parser);

        Assert.Null(ordinal);
    }

    [Fact(DisplayName = "blank")]
    public void Blank()
    {
        Token[] tokens = { Sentinel.Instance };
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Ordinal.Parse(ref parser);

        Assert.Null(arguments);
    }

    [Fact(DisplayName = "bad component")]
    public void BadComponent()
    {
        // [test, (thing;stuff)]

        Token[] tokens =
        {
            new StartOrdinal { sourcecode = new[] { StartOrdinal.symbol } },
            new Word { sourcecode = "test".AsMemory() },
            new Separator { sourcecode = new[] { Separator.symbol } },
            new Word { sourcecode = "thing".AsMemory() },
            new Terminal { sourcecode = new[] { Terminal.symbol } },
            new Word { sourcecode = "stuff".AsMemory() },
            new EndValues { sourcecode = new[] { EndValues.symbol } },
            new EndOrdinal { sourcecode = new[] { EndOrdinal.symbol } },
        };
        
        Parser parser = new(tokens);
        var ordinal = Ronin.Grammar.Compound.Ordinal.Parse(ref parser);

        Assert.Null(ordinal);
    }

    [Fact(DisplayName = "terminated incorrectly")]
    public void TerminatedWrong()
    {
        // [test;]

        Token[] tokens =
        {
            new StartOrdinal { sourcecode = new[] { StartOrdinal.symbol } },
            new Word { sourcecode = "test".AsMemory() },
            new Terminal { sourcecode = new[] { Terminal.symbol } },
            new EndOrdinal { sourcecode = new[] { EndOrdinal.symbol } },
        };
        
        Parser parser = new(tokens);
        var ordinal = Ronin.Grammar.Compound.Ordinal.Parse(ref parser);

        Assert.Null(ordinal);
    }
}
