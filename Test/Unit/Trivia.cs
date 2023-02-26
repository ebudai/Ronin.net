using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class trivia
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // ;

        Token[] tokens =
        {
            new Whitespace(),
            new Terminal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var trivia = TriviaSyntax.Parse(ref parser);
        Assert.NotNull(trivia);
    }
}
