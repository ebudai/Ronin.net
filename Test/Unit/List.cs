using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;
using Test;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class list
{
    [Fact(DisplayName = "single")]
    public void Single()
    {
        Tokens tokens = new();
        tokens.Add<OpenBrace>()
            .Add<Number>("3")
            .Add<CloseBrace>();

        Parser parser = new(tokens.ToArray());
        var list = List.Parse(ref parser);

        Assert.Single(list?.Values);
        Scalar scalar = list.Values[0];
        Assert.Single(scalar?.Literals);
        Assert.Equal("3", scalar.Literals[0]?.ToString());
    }

    [Fact(DisplayName = "multiple")]
    public void Multiple()
    {
        Tokens tokens = new();
        tokens.Add<OpenBrace>()
            .Add<Number>("1")
            .Add<Separator>()
            .Add<Number>("2")
            .Add<Separator>()
            .Add<Number>("6")
            .Add<CloseBrace>();

        Parser parser = new(tokens.ToArray());
        var list = List.Parse(ref parser);

        Assert.Equal(3, list?.Values?.Count);

        {
            Scalar scalar = list.Values[0];
            Assert.Single(scalar?.Literals);
            Assert.Equal("1", scalar.Literals[0]?.ToString());
        }

        {
            Scalar scalar = list.Values[1];
            Assert.Single(scalar?.Literals);
            Assert.Equal("2", scalar.Literals[0]?.ToString());
        }

        {
            Scalar scalar = list.Values[2];
            Assert.Single(scalar?.Literals);
            Assert.Equal("6", scalar.Literals[0]?.ToString());
        }
    }
}
