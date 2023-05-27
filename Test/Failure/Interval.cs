using Ronin.Compiler;
using Ronin.Lexicon;

namespace Failure;

[Trait("Parser", null)]
public class Interval
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
            new Terminal { sourcecode = Terminal.symbol.AsMemory() }
        };
        
        Parser parser = new(tokens);
        var interval = Ronin.Grammar.Interval.Parse(ref parser);

        Assert.Null(interval);
    }

    [Fact(DisplayName = "missing both start and end")]
    public void MissingStartAndEnd()
    {
        // ..

        Token[] tokens =
        {
            new Ronin.Lexicon.Punctuation.Range { sourcecode = Ronin.Lexicon.Punctuation.Range.symbol.AsMemory() },
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var reference = Ronin.Grammar.Reference.Parse(ref parser);

        Assert.Null(reference);
    }
}
