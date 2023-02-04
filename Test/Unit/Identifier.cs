using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;
using Test;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class identifier
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        Tokens tokens = new();
        tokens.Add<Word>("test")
            .Add<OpenParenthesis>()
            .Add<Word>("thing")
            .Add<Returns>()
            .Add<Word>("number")
            .Add<CloseParenthesis>();

        Parser parser = new(tokens.ToArray());
        var identifier = Identifier.Parse(ref parser);

        Assert.Equal(2, identifier?.Components?.Count);

        Name name = identifier.Components[0];
        Assert.Equal("test", string.Join(" ", name?.Words ?? new List<string>()));

        Parameters parameters = identifier.Components[1];
        Assert.Single(parameters?.Values);
        Datum datum = parameters.Values[0];
        Assert.Equal("thing", string.Join(" ", datum?.Name?.Words ?? new List<string>()));
        Assert.Single(datum?.Datatype?.Components);
        name = datum.Datatype.Components[0];
        Assert.Equal("number", string.Join(" ", name?.Words ?? new List<string>()));
    }
}

