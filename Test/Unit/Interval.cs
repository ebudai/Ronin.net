using Ronin;
using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

namespace Unit;

[Trait("Parser", null)]
public class Interval
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // 3..4

        Token[] tokens =
        {
            new NumberLiteral(),
            new Ronin.Lexicon.Punctuation.Range(),
            new NumberLiteral(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var interval = Ronin.Grammar.Interval.Parse(ref parser);

        Ronin.Grammar.Literal start = interval.Start;
        Assert.Equal(1, start?.Source.Length);

        Ronin.Grammar.Literal end = interval.End;
        Assert.Equal(1, end?.Source.Length);
    }

    [Fact(DisplayName = "left unspecified")]
    public void LeftUnspecified()
    {
        // ..3

        Token[] tokens =
        {
            new Ronin.Lexicon.Punctuation.Range(),
            new NumberLiteral(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var interval = Ronin.Grammar.Interval.Parse(ref parser);

        Assert.Null(interval.Start);

        Ronin.Grammar.Literal end = interval.End;
        Assert.Equal(1, end?.Source.Length);
    }

    [Fact(DisplayName = "right unspecified")]
    public void RightUnspecified() 
    {
        // 7..

        Token[] tokens =
        {
            new NumberLiteral(),
            new Ronin.Lexicon.Punctuation.Range(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var interval = Ronin.Grammar.Interval.Parse(ref parser);

        Ronin.Grammar.Literal start = interval.Start;
        Assert.Equal(1, start?.Source.Length);

        Assert.Null(interval.End);
    }
}
