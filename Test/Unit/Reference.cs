using Ronin;
using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Punctuation;

namespace Unit;

[Trait("Parser", null)]
public class Reference
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // thing 7 ("stuff")

        Token[] tokens =
        {
            new Word(),
            new NumberLiteral(),
            new OpenParenthesis(),
            new TextLiteral(),
            new CloseParenthesis(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var reference = Ronin.Grammar.Reference.Parse(ref parser);

        Assert.Equal(3, reference?.Components?.Count);

        {
            Ronin.Grammar.Name name = reference.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            Ronin.Grammar.Literal scalar = reference.Components[1];
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            Ronin.Grammar.Compound.Arguments arguments = reference.Components[2];
            Assert.Single(arguments?.Values);
            Ronin.Grammar.Literal scalar = arguments.Values[0];
            Assert.Equal(1, scalar?.Source.Length);
        }
    }
}
