using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Punctuation;

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
        var ordinal = Ronin.Grammar.Interval.Parse(ref parser);

        Assert.Null(ordinal);
    }
}
