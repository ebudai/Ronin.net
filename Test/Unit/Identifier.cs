using Ronin;
using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

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
            new StartValues(),
            new Word(),
            new Returns(),
            new Word(),
            new EndValues(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var identifier = Ronin.Grammar.Name.Parse(ref parser);

        Assert.Equal(2, identifier?.Components?.Count);

        {
            Ronin.Grammar.Words name = identifier.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            Ronin.Grammar.Compound.Parameters parameters = identifier.Components[1];
            Assert.Single(parameters?.Values);
            Ronin.Grammar.DatumDeclaration datum = parameters.Values[0];
            Assert.Single(datum?.Name?.Components);

            Assert.Single(datum?.Datatype?.Components);
            Ronin.Grammar.Words name = datum.Datatype.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }        
    }
}

