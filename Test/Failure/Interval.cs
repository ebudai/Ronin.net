using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;
using Test;

namespace Failure;

[Trait("Parser", null)]
public class Intervals : ParsingTests
{
    [Fact(DisplayName = "not an interval")]
    public void NotAnInterval()
    {
        // not an interval;

        List<Token> tokens = new()
        {
            Word("not"),
            Word("an"),
            Word("interval"),
            Terminal()
        };
        
        Parser parser = new(tokens);
        var interval = Interval.Parse(ref parser);

        Assert.Null(interval);
    }

    [Fact(DisplayName = "missing both start and end")]
    public void MissingStartAndEnd()
    {
        // ..

        List<Token> tokens = new()
        {
            Range(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var reference = Reference.Parse(ref parser);

        Assert.Null(reference);
    }
}
