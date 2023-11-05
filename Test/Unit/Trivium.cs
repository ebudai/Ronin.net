using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait(nameof(Parser), null)]
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
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var trivia = Trivia.Parse(ref parser);
        Assert.NotNull(trivia);
    }
}
