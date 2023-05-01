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
            new Word(),
            new Word(),
            new Word(),
            new Terminal()
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
            new Ronin.Lexicon.Punctuation.Range(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var interval = Ronin.Grammar.Interval.Parse(ref parser);

        Assert.Null(interval);
    }
}
