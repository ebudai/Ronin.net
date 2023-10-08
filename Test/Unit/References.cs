using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;
using Literal = Ronin.Grammar.Literal;

namespace Unit;

[Trait(nameof(Parser), null)]
public class References : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // thing 7 ("stuff")

        List<Token> tokens = new()
        {
            Word("thing"),
            Number(7),
            StartValues(),
            Text("stuff"),
            EndValues(),
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var reference = Reference.Parse(ref parser);

        Assert.Equal(3, reference?.Components?.Count);

        {
            Name name = reference.Components[0].AsT0;
            Assert.Single(name?.Tokens.ToArray());
        }

        {
            var scalar = reference.Components[1].AsT1 as Literal;
            Assert.Single(scalar?.Tokens.ToArray());
        }

        {
            var arguments = reference.Components[2].AsT1 as Inputs;
            Assert.Single(arguments);
            var scalar = arguments[0].AsT0 as Literal;
            Assert.Single(scalar?.Tokens.ToArray());
        }
    }
}
