using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Literals;

using Range = Ronin.Lexicon.Symbols.Range;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class interval
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        Token[] tokens =
        {
            new Number(),
            new Range(),
            new Number(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var interval = Interval.Parse(ref parser);

        Scalar start = interval.Start;
        Assert.Single(start?.Source);
        
        Scalar end = interval.End;
        Assert.Single(end?.Source);
    }

    [Fact(DisplayName = "left unspecified")]
    public void LeftUnspecified()
    {
        Token[] tokens =
        {
            new Range(),
            new Number(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var interval = Interval.Parse(ref parser);

        Assert.Null(interval.Start);

        Scalar end = interval.End;
        Assert.Single(end?.Source);
    }

    [Fact(DisplayName = "right unspecified")]
    public void RightUnspecified() 
    {
        Token[] tokens =
        {
            new Number(),
            new Range(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var interval = Interval.Parse(ref parser);

        Scalar start = interval.Start;
        Assert.Single(start?.Source);

        Assert.Null(interval.End);
    }
}
