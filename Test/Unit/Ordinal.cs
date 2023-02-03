using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;
using Test;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class ordinal
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        Tokens tokens = new();
        tokens.Add<Variable>()
            .Add<Word>("test")
            .Add<Returns>()
            .Add<Word>("number")
            .Add<OpenSquareBracket>()
            .Add<Number>("4")
            .Add<CloseSquareBracket>()
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var datum = Datum.Parse(ref parser);

        Assert.NotNull(datum?.Datatype?.Ordinal);
        Assert.Single(datum.Datatype.Ordinal.Values);
        Scalar scalar = datum.Datatype.Ordinal.Values[0];
        Assert.Single(scalar.Literals);
        Assert.Equal("4", scalar.Literals[0].ToString());
    }
}
