using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Trivium : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // ;

        List<Token> tokens = new()
        {
            Whitespace(),
            Terminal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var trivia = Trivia.Parse(ref parser);
        Assert.NotNull(trivia);
    }
}
