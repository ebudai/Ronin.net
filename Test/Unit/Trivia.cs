using Ronin.Compiler;
using Ronin.Lexicon;

namespace Unit;

[Trait("Parser", null)]
public class Trivia
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // ;

        Token[] tokens =
        {
            new Ronin.Lexicon.Whitespace(),
            new Terminal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var trivia = Ronin.Grammar.Trivia.Parse(ref parser);
        Assert.NotNull(trivia);
    }
}
