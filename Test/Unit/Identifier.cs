using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;
using Test;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class identifier
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        Tokens tokens = new();
        tokens.Add<Word>("test")
            .Add<OpenParenthesis>()
            .Add<Word>("thing")
            .Add<Returns>()
            .Add<Word>("number")
            .Add<CloseParenthesis>();

        Parser parser = new(tokens.ToArray());
        var identifier = Identifier.Parse(ref parser);

        Assert.Equal(2, identifier?.Components?.Count);

        {
            Name name = identifier.Components[0];
            Assert.Single(name?.Words);
            Assert.Equal("test", name.Words[0]);
        }

        {
            Parameters parameters = identifier.Components[1];
            Assert.Single(parameters?.Values);
            Datum datum = parameters.Values[0];
            Assert.Single(datum?.Name?.Words);
            Assert.Equal("thing", datum.Name.Words[0]);
            Assert.Single(datum?.Datatype?.Components);
            Name name = datum.Datatype.Components[0];
            Assert.Single(name?.Words);
            Assert.Equal("number", name.Words[0]);
        }        
    }
}

