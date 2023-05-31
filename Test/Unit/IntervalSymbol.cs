using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Literals;

namespace Unit;

[Trait("Parser", null)]
public class IntervalSymbol
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // 3..4

        Token[] tokens =
        {
            new Number(),
            new Ronin.Lexicon.Symbols.Range(),
            new Number(),
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

        Token[] tokens =
        {
            new Ronin.Lexicon.Symbols.Range(),
            new Number(),
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

        Token[] tokens =
        {
            new Number(),
            new Ronin.Lexicon.Symbols.Range(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var reference = Ronin.Grammar.Reference.Parse(ref parser);

        Assert.Equal(2, reference?.Components.Count);
        Ronin.Grammar.Interval interval = reference.Components[1];
        Assert.NotNull(interval);
    }
}
