using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Symbols;
using Test;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class list
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        Tokens tokens = new();
        tokens.Add<Variable>()
            .Add<Word>("x")
            .Add<Returns>()
            .Add<Word>("number")
            .Add<OpenSquareBracket>()
            .Add<CloseSquareBracket>()
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var datum = Datum.Parse(ref parser);

        Assert.NotNull(datum?.Datatype?.Ordinal);
        Assert.Empty(datum.Datatype.Ordinal.Values);
    }
}
