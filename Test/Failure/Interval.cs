using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Parser", null)]
public class Intervals
{
    [Fact(DisplayName = "not an interval")]
    public void NotAnInterval()
    {
        // not an interval;

        Token[] tokens =
        {
            new Word { sourcecode = "not".AsMemory() },
            new Word { sourcecode = "an".AsMemory() },
            new Word { sourcecode = "interval".AsMemory() },
            new Terminal { sourcecode = new[] { Terminal.symbol } }
        };
        
        Parser parser = new(tokens);
        var interval = Interval.Parse(ref parser);

        Assert.Null(interval);
    }

    [Fact(DisplayName = "missing both start and end")]
    public void MissingStartAndEnd()
    {
        // ..

        Token[] tokens =
        {
            new Ronin.Lexicon.Symbols.Range { sourcecode = Ronin.Lexicon.Symbols.Range.symbol.AsMemory() },
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var reference = Reference.Parse(ref parser);

        Assert.Null(reference);
    }
}
