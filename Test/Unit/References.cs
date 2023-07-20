using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Compound;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait("Parser", null)]
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
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var reference = Reference.Parse(ref parser);

        Assert.Equal(3, reference?.Components?.Count);

        {
            Name name = reference.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            AnonymousValue scalar = reference.Components[1];
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            AnonymousValue anonymous = reference.Components[2];
            var arguments = anonymous as Inputs;
            Assert.Single(arguments?.Values);
            Value value = arguments.Values[0];
            var scalar = value as Inline;
            Assert.Equal(1, scalar?.Source.Length);
        }
    }
}
