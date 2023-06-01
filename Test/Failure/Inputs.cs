using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Parser", null)]
public class Inputs
{
    [Fact(DisplayName = "does not start with (")]
    public void NotAnArguments()
    {
        // not an object;

        Token[] tokens = 
        {
            new Word { sourcecode = "not".AsMemory() },
            new Word { sourcecode = "an".AsMemory() },
            new Word { sourcecode = "object".AsMemory() },
            new Terminal { sourcecode = new[] { Terminal.symbol } },
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Inputs.Parse(ref parser);

        Assert.Null(arguments);
    }

    [Fact(DisplayName = "blank")]
    public void Blank()
    {
        Token[] tokens = { Sentinel.Instance };
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Inputs.Parse(ref parser);

        Assert.Null(arguments);
    }

    [Fact(DisplayName = "bad separator")]
    public void BadSeparator()
    {
        // (test, (thing;stuff))

        Token[] tokens =
        {
            new StartValues { sourcecode = new[] { StartValues.symbol } },
            new Word { sourcecode = "test".AsMemory() },
            new Separator { sourcecode = new[] { Separator.symbol } },
            new StartValues { sourcecode = new[] { StartValues.symbol } },
            new Word { sourcecode = "thing".AsMemory() },
            new Terminal { sourcecode = new[] { Terminal.symbol } },
            new Word { sourcecode = "stuff".AsMemory() },
            new EndValues { sourcecode = new[] { EndValues.symbol } },
            new EndValues { sourcecode = new[] { EndValues.symbol } },
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Inputs.Parse(ref parser);
        
        Assert.Null(arguments);
    }

    [Fact(DisplayName = "terminated incorrectly")]
    public void TerminatedWrong()
    {
        // (test;)

        Token[] tokens =
        {
            new StartValues { sourcecode = new[] { StartValues.symbol } },
            new Word { sourcecode = "test".AsMemory() },
            new Terminal { sourcecode = new[] { Terminal.symbol } },
            new EndValues { sourcecode = new[] { EndValues.symbol } },
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Inputs.Parse(ref parser);
        
        Assert.Null(arguments);
    }
}
