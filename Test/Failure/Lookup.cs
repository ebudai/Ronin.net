using Ronin.Compiler;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;
using Test;

namespace Failure;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class lookup
{
    [Fact(DisplayName = "missing assign")]
    public void MissingAssign()
    {
        Tokens tokens = new();
        tokens.Add<OpenBrace>()
            .Add<Text>("\"test\"")
            .Add<Number>("3")
            .Add<CloseBrace>();

        Parser parser = new(tokens.ToArray());
        var lookup = Lookup.Parse(ref parser);

        Assert.IsNotType<Lookup>(lookup);
    }

    [Fact(DisplayName = "missing key")]
    public void MissingKey()
    {
        Tokens tokens = new();
        tokens.Add<OpenBrace>()
            .Add<Assign>()
            .Add<Number>("3")
            .Add<CloseBrace>();

        Parser parser = new(tokens.ToArray());
        var lookup = Lookup.Parse(ref parser);

        Assert.IsNotType<Lookup>(lookup);
    }

    [Fact(DisplayName = "missing value")]
    public void MissingValue()
    {
        Tokens tokens = new();
        tokens.Add<OpenBrace>()
            .Add<Number>("3")
            .Add<Assign>()
            .Add<CloseBrace>();

        Parser parser = new(tokens.ToArray());
        var lookup = Lookup.Parse(ref parser);

        Assert.IsNotType<Lookup>(lookup);
    }


}
