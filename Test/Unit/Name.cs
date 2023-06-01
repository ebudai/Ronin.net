using Ronin.Compiler;
using Ronin.Lexicon;

namespace Unit;

[Trait("Parser", null)]
public class Name
{
    [Fact(DisplayName = "symbols")]
    public void Symbols()
    {
        // name + things

        Token[] tokens = 
        {
            new Word(),
            new Ronin.Lexicon.Symbol(),
            new Word(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var name = Ronin.Grammar.Words.Parse(ref parser);

        Assert.Equal(3, name?.Source.Length);
    }
}
