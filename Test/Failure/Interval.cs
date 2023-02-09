using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

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

        Tokens tokens = new();
        tokens.Add<Word>("not")
            .Add<Word>("an")
            .Add<Word>("interval")
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var ordinal = Interval.Parse(ref parser);

        Assert.Null(ordinal);
    }
}
