using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

namespace Failure;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class interval
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
        var ordinal = IntervalSyntax.Parse(ref parser);

        Assert.Null(ordinal);
    }
}
