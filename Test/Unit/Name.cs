using Ronin;
using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Punctuation;

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
            new Plus(),
            new Word(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var name = Ronin.Grammar.Name.Parse(ref parser);

        Assert.Equal(3, name?.Source.Length);
    }
}
