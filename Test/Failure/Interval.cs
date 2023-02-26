using Ronin.Compiler;
using Ronin.Grammar;
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
            new TerminalSymbol()
        };
        
        Parser parser = new(tokens);
        var ordinal = IntervalSyntax.Parse(ref parser);

        Assert.Null(ordinal);
    }
}
