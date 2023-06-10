using Ronin.Compiler;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Trivia : ParsingTests
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
        var trivia = Ronin.Grammar.Trivia.Parse(ref parser);
        Assert.NotNull(trivia);
    }
}
