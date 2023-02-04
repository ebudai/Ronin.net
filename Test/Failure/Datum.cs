using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;
using Test;

namespace Failure;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class datum
{
    [Fact(DisplayName = $"{Reactive.keyword} before name")]
    public void ReturnsBeforeName()
    {
        Tokens tokens = new();
        tokens.Add<Reactive>()
            .Add<Returns>()
            .Add<Number>("44.3")
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var datum = Datum.Parse(ref parser);
        Assert.Null(datum);
    }

    [Fact(DisplayName = "literal instead of identifier")]
    public void LiteralInsteadOfIdentifier()
    {
        Tokens tokens = new();
        tokens.Add<Variable>()
            .Add<Number>("555")
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var datum = Datum.Parse(ref parser);
        Assert.Null(datum);
    }
}

