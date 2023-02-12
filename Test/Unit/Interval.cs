using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon.Literals;
using Test;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class interval
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string two = "2";
        const string sixteen = "16";

        Tokens tokens = new();
        tokens.Add<Number>(two)
            .Add<Ronin.Lexicon.Symbols.Range>()
            .Add<Number>(sixteen);

        Parser parser = new(tokens.ToArray());
        var interval = Interval.Parse(ref parser);

        Scalar start = interval.Start;
        Assert.Single(start?.Literals);
        Assert.Equal(two, start.Literals[0]?.Sourcecode.ToString());

        Scalar end = interval.End;
        Assert.Single(end?.Literals);
        Assert.Equal(sixteen, end.Literals[0]?.Sourcecode.ToString());
    }

    [Fact(DisplayName = "left unspecified")]
    public void LeftUnspecified()
    {
        const string four = "4";

        Tokens tokens = new();
        tokens.Add<Ronin.Lexicon.Symbols.Range>().Add<Number>(four);

        Parser parser = new(tokens.ToArray());
        var interval = Interval.Parse(ref parser);

        Assert.Null(interval.Start);

        Scalar end = interval.End;
        Assert.Single(end?.Literals);
        Assert.Equal(four, end.Literals[0]?.Sourcecode.ToString());
    }

    [Fact(DisplayName = "right unspecified")]
    public void RightUnspecified() 
    {
        const string twelve = "12";

        Tokens tokens = new();
        tokens.Add<Number>(twelve).Add<Ronin.Lexicon.Symbols.Range>();

        Parser parser = new(tokens.ToArray());
        var interval = Interval.Parse(ref parser);

        Scalar start = interval.Start;
        Assert.Single(start?.Literals);
        Assert.Equal(twelve, start.Literals[0]?.Sourcecode.ToString());

        Assert.Null(interval.End);
    }
}
