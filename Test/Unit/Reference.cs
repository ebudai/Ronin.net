using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

namespace Unit;

[Trait("Parser", null)]
public class Reference
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // thing 7 (stuff)

        Token[] tokens =
        {
            new Word(),
            new NumberLiteral(),
            new OpenParenthesisSymbol(),
            new TextLiteral(),
            new CloseParenthesisSymbol(),
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
            LiteralSyntax scalar = reference.Components[1];
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            Ronin.Grammar.Aggregates.Arguments arguments = reference.Components[2];
            Assert.Single(arguments?.Values);
            LiteralSyntax scalar = arguments.Values[0];
            Assert.Equal(1, scalar?.Source.Length);
        }
    }
}
