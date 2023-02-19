using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Symbols;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class parameters
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // (var test => money)

        Token[] tokens = 
        {
            new OpenParenthesis(),
            new Variable(),
            new Word(),
            new Returns(),
            new Word(),
            new CloseParenthesis(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var parameters = Parameters.Parse(ref parser);

        Assert.Single(parameters?.Values);

        Datum datum = parameters.Values[0];

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.False(datum.Is.Compiled);
        Assert.False(datum.Is.Optional);
        Assert.False(datum.Is.Persistent);
        Assert.False(datum.Is.Shared);

        Assert.Single(datum.Name?.Words);

        Name name = datum?.Datatype?.Components?[0];
        Assert.Single(name?.Words);
    }

    [Fact(DisplayName = "multiple")]
    public void Multiple()
    {
        // (test => number, stuff in things => text)

        Token[] tokens = 
        {
            new OpenParenthesis(),
            new Word(),
            new Returns(),
            new Word(),
            new Separator(),
            new Word(),
            new Word(),
            new Word(),
            new Returns(),
            new Word(),
            new CloseParenthesis(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var parameters = Parameters.Parse(ref parser);

        Assert.Equal(2, parameters?.Values?.Count);

        {
            Datum datum = parameters.Values[0];
            
            Assert.Null(datum?.Mutability);

            Assert.False(datum.Is.Compiled);
            Assert.False(datum.Is.Optional);
            Assert.False(datum.Is.Persistent);
            Assert.False(datum.Is.Shared);

            Assert.Single(datum.Name?.Words);
        
            Assert.Single(datum.Datatype?.Components);
            Name name = datum.Datatype.Components[0];
            Assert.Single(name?.Words);
        }

        {
            Datum datum = parameters.Values[1];

            Assert.Null(datum?.Mutability);

            Assert.False(datum.Is.Compiled);
            Assert.False(datum.Is.Optional);
            Assert.False(datum.Is.Persistent);
            Assert.False(datum.Is.Shared);

            Assert.Single(datum.Datatype?.Components);
            Name name = datum.Datatype.Components[0];
            Assert.Single(name?.Words);
        }
    }

    [Fact(DisplayName = "empty parenthesis")]
    public void Empty()
    {
        // ()

        Token[] tokens = 
        {
            new OpenParenthesis(),
            new CloseParenthesis(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var arguments = Parameters.Parse(ref parser);

        Assert.Empty(arguments?.Values);
    }
}
