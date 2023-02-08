using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;
using Test;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class interval
{
    [Fact(DisplayName = "closed")]
    public void Closed()
    {
        Test("2", "7");
    }

    [Fact(DisplayName = "open")]
    public void Open()
    {
        Test("0", "18");
    }

    [Fact(DisplayName = "left open")]
    public void LeftOpen()
    {
        Test("-100", "0");
    }

    [Fact(DisplayName = "right open")]
    public void RightOpen()
    {
        Test("1002", "1003");
    }

    private static void Test(string startvalue, string endvalue)
    {
        Tokens tokens = new();
        tokens.Add<Number>(startvalue)
            .Add<Ronin.Lexicon.Symbols.Range>()
            .Add<Number>(endvalue);

        Parser parser = new(tokens.ToArray());
        var interval = Interval.Parse(ref parser);

        Scalar start = interval.Start;
        Assert.Single(start?.Literals);
        Assert.Equal(startvalue, start.Literals[0]?.ToString());

        Scalar end = interval.End;
        Assert.Single(end?.Literals);
        Assert.Equal(endvalue, end.Literals[0]?.ToString());
    }
}
