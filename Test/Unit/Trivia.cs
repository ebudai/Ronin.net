using Ronin.Compiler;
using Ronin.Grammar;
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
            new TerminalSymbol(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var trivia = TriviaSyntax.Parse(ref parser);
        Assert.NotNull(trivia);
    }
}
