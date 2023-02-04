using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;
using Test;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class name
{
    [Fact(DisplayName = "symbols")]
    public void Symbols()
    {
        // name + things

        Tokens tokens = new();
        tokens.Add<Word>("name")
            .Add<Plus>()
            .Add<Word>("things");

        Parser parser = new(tokens.ToArray());
        var name = Name.Parse(ref parser);

        Assert.Equal("name + things", string.Join(" ", name?.Words ?? new List<string>()));
    }
}
