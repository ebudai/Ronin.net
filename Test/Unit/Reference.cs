using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class reference
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // thing 7 (stuff)

        Token[] tokens =
        {
            new Word(),
            new Number(),
            new OpenParenthesis(),
            new Text(),
            new CloseParenthesis(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var reference = Reference.Parse(ref parser);

        Assert.Equal(3, reference?.Components?.Count);

        {
            Name name = reference.Components[0];
            Assert.Single(name?.Source);
        }

        {
            Scalar scalar = reference.Components[1];
            Assert.Single(scalar?.Source);
        }

        {
            Arguments arguments = reference.Components[2];
            Assert.Single(arguments?.Values);
            Scalar scalar = arguments.Values[0];
            Assert.Single(scalar?.Source);
        }
    }
}
