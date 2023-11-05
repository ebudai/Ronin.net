using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait(nameof(Parser), null)]
public class Intervals : ParsingTests
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
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var interval = Value.Parse(ref parser);

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
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var reference = Reference.Parse(ref parser);

        Assert.Equal(2, reference?.Span.Length);
    }

    [Fact(DisplayName = "right unspecified")]
    public void RightUnspecified() 
    {
        // 7..

        List<Token> tokens = new()
        {
            Number(7),
            Range(),
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var reference = Reference.Parse(ref parser);

        Assert.Equal(2, reference?.Span.Length);
    }
}
