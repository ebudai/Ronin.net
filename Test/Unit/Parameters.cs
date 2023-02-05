using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Symbols;
using Test;

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

        Tokens tokens = new();
        tokens.Add<OpenParenthesis>()
            .Add<Variable>()
            .Add<Word>("test")
            .Add<Returns>()
            .Add<Word>("money")
            .Add<CloseParenthesis>();

        Parser parser = new(tokens.ToArray());
        var parameters = Parameters.Parse(ref parser);

        Assert.Single(parameters?.Values);

        Datum datum = parameters.Values[0];

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.False(datum.Is.Compiled);
        Assert.False(datum.Is.Optional);
        Assert.False(datum.Is.Persistent);
        Assert.False(datum.Is.Shared);

        Assert.Single(datum.Name?.Words);
        Assert.Equal("test", datum.Name.Words[0]);

        Name name = datum?.Datatype?.Components?[0];
        Assert.Equal("money", name?.Words?[0]);
    }

    [Fact(DisplayName = "multiple")]
    public void Multiple()
    {
        // (test => number, stuff in things => text)

        Tokens tokens = new();
        tokens.Add<OpenParenthesis>()
            .Add<Word>("test")
            .Add<Returns>()
            .Add<Word>("number")
            .Add<Separator>()
            .Add<Word>("stuff")
            .Add<Word>("in")
            .Add<Word>("things")
            .Add<Returns>()
            .Add<Word>("text")
            .Add<CloseParenthesis>();

        Parser parser = new(tokens.ToArray());
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
            Assert.Equal("test", datum.Name.Words[0]);

            Assert.Single(datum.Datatype?.Components);
            Name name = datum.Datatype.Components[0];
            Assert.Single(name?.Words);
            Assert.Equal("number", name.Words[0]);
        }

        {
            Datum datum = parameters.Values[1];

            Assert.Null(datum?.Mutability);

            Assert.False(datum.Is.Compiled);
            Assert.False(datum.Is.Optional);
            Assert.False(datum.Is.Persistent);
            Assert.False(datum.Is.Shared);

            Assert.Equal("stuff in things", string.Join(" ", datum?.Name?.Words ?? new List<string>()));

            Assert.Single(datum.Datatype?.Components);
            Name name = datum.Datatype.Components[0];
            Assert.Single(name?.Words);
            Assert.Equal("text", name.Words[0]);
        }
    }

    [Fact(DisplayName = "empty parenthesis")]
    public void Empty()
    {
        // ()

        Tokens tokens = new();
        tokens.Add<OpenParenthesis>().Add<CloseParenthesis>();

        Parser parser = new(tokens.ToArray());
        var arguments = Parameters.Parse(ref parser);

        Assert.Empty(arguments?.Values);
    }
}
