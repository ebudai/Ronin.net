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
        Tokens tokens = new();
        tokens.Add<Word>("name")
            .Add<Plus>()
            .Add<Word>("things")
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var reference = Reference.Parse(ref parser);

        Assert.NotNull(reference);
        Assert.Single(reference.Components);
        Name name = reference.Components[0];
        Assert.Equal(3, name.Words.Count);
        Assert.Equal("name", name.Words[0]);
        Assert.Equal("+", name.Words[1]);
        Assert.Equal("things", name.Words[2]);
    }
}
