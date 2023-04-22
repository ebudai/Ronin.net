using Ronin;
using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Punctuation;

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
            new OpenParenthesis(),
            new Word(),
            new Returns(),
            new Word(),
            new CloseParenthesis(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var identifier = Ronin.Grammar.Identifier.Parse(ref parser);

        Assert.Equal(2, identifier?.Components?.Count);

        {
            Ronin.Grammar.Name name = identifier.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            Ronin.Grammar.Compound.Parameters parameters = identifier.Components[1];
            Assert.Single(parameters?.Values);
            Ronin.Grammar.Datum datum = parameters.Values[0];
            Assert.Equal(1, datum?.Name?.Source.Length);

            Assert.Single(datum?.Datatype?.Components);
            Ronin.Grammar.Name name = datum.Datatype.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }        
    }
}

