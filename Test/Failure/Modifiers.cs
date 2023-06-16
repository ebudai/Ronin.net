using Ronin.Compiler;
using Ronin.Lexicon;
using Test;

namespace Failure;

[Trait("Parser", null)]
public class Modifiers : ParsingTests
{
    [Fact(DisplayName = "not reactive")]
    public void NotAModifier()
    {
        // thingy => 44.3;

        List<Token> tokens = new()
        {
            Word("thingy"),
            Returns(),
            Number(44.3),
            Terminal(),
        };

        Parser parser = new(tokens);
        var modifier = Ronin.Grammar.Modifiers.Parse(ref parser);

        Assert.Null(modifier);
    }
}
