using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

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

        Token[] tokens = 
        {
            new Word(),
            new Plus(),
            new Word(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var name = Name.Parse(ref parser);

        Assert.Equal(3, name?.Words?.Count);
    }
}
