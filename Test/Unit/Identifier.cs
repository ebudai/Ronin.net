using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class identifier
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
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
        var identifier = Identifier.Parse(ref parser);

        Assert.Equal(2, identifier?.Components?.Count);

        {
            Name name = identifier.Components[0];
            Assert.Single(name?.Source);
        }

        {
            Parameters parameters = identifier.Components[1];
            Assert.Single(parameters?.Values);
            Datum datum = parameters.Values[0];
            Assert.Single(datum?.Name?.Source);

            Assert.Single(datum?.Datatype?.Components);
            Name name = datum.Datatype.Components[0];
            Assert.Single(name?.Source);
        }        
    }
}

