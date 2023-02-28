using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

namespace Unit;

[Trait("Parser", null)]
public class Identifier
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // x (y => number)

        Token[] tokens =
        {
            new Word(),
            new OpenParenthesisSymbol(),
            new Word(),
            new ReturnsSymbol(),
            new Word(),
            new CloseParenthesisSymbol(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var identifier = IdentifierSyntax.Parse(ref parser);

        Assert.Equal(2, identifier?.Components?.Count);

        {
            Ronin.Grammar.Name name = identifier.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            Ronin.Grammar.Aggregates.Parameters parameters = identifier.Components[1];
            Assert.Single(parameters?.Values);
            DatumDeclarationSyntax datum = parameters.Values[0];
            Assert.Equal(1, datum?.Name?.Source.Length);

            Assert.Single(datum?.Datatype?.Components);
            Ronin.Grammar.Name name = datum.Datatype.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }        
    }
}

