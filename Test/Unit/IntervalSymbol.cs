using Ronin.Compiler;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class IntervalSymbol : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // 3..4

        List<Token> tokens = new()
        {
            Number(3),
            Range(),
            Number(4),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var reference = Ronin.Grammar.Reference.Parse(ref parser);

        Assert.Equal(3, reference?.Components.Count);
        Ronin.Grammar.Interval interval = reference.Components[1];
        Assert.NotNull(interval);
    }

    [Fact(DisplayName = "left unspecified")]
    public void LeftUnspecified()
    {
        // ..3

        List<Token> tokens = new()
        {
            Range(),
            Number(3),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var interval = Ronin.Grammar.Interval.Parse(ref parser);

        Assert.NotNull(interval);
    }

    [Fact(DisplayName = "right unspecified")]
    public void RightUnspecified() 
    {
        // 7..

        List<Token> tokens = new()
        {
            Number(7),
            Range(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var reference = Ronin.Grammar.Reference.Parse(ref parser);

        Assert.Equal(2, reference?.Components.Count);
        Ronin.Grammar.Interval interval = reference.Components[1];
        Assert.NotNull(interval);
    }
}
