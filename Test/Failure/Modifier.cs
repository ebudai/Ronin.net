using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Failure;

[Trait("Parser", null)]
public class Modifier : ParsingTests
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
        var modifier = Modifiers.Parse(ref parser);

        Assert.Null(modifier);
    }
}
