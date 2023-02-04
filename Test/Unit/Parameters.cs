using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
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

        Assert.NotNull(parameters);

        Assert.Single(parameters.Values);
        Datum datum = parameters.Values[0];
        Assert.NotNull(datum);

        Assert.IsType<Variable>(datum.Mutability);

        Assert.False(datum.Is.Compiled);
        Assert.False(datum.Is.Optional);
        Assert.False(datum.Is.Persistent);
        Assert.False(datum.Is.Shared);

        Assert.NotNull(datum.Name);
        Assert.Single(datum.Name.Words);
        Assert.Equal("test", datum.Name.Words[0]);

        Assert.NotNull(datum.Datatype);
        Assert.Single(datum.Datatype.Components);
        Name name = datum.Datatype.Components[0];
        Assert.NotNull(name);
        Assert.Single(name.Words);
        Assert.Equal("money", name.Words[0]);
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

        Assert.NotNull(parameters);
        Assert.NotNull(parameters.Values);
        Assert.Equal(2, parameters.Values.Count);

        {
            Datum datum = parameters.Values[0];
            Assert.NotNull(datum);

            Assert.Null(datum.Mutability);

            Assert.False(datum.Is.Compiled);
            Assert.False(datum.Is.Optional);
            Assert.False(datum.Is.Persistent);
            Assert.False(datum.Is.Shared);

            Assert.NotNull(datum.Name);
            Assert.Single(datum.Name.Words);
            Assert.Equal("test", datum.Name.Words[0]);

            Assert.NotNull(datum.Datatype);
            Assert.Single(datum.Datatype.Components);
            Name name = datum.Datatype.Components[0];
            Assert.NotNull(name);
            Assert.Single(name.Words);
            Assert.Equal("number", name.Words[0]);
        }

        {
            Datum datum = parameters.Values[1];
            Assert.NotNull(datum);

            Assert.Null(datum.Mutability);

            Assert.False(datum.Is.Compiled);
            Assert.False(datum.Is.Optional);
            Assert.False(datum.Is.Persistent);
            Assert.False(datum.Is.Shared);

            Assert.NotNull(datum.Name);
            Assert.NotNull(datum.Name.Words);
            Assert.Equal(3, datum.Name.Words.Count);
            Assert.Equal("stuff", datum.Name.Words[0]);
            Assert.Equal("in", datum.Name.Words[1]);
            Assert.Equal("things", datum.Name.Words[2]);

            Assert.NotNull(datum.Datatype);
            Assert.Single(datum.Datatype.Components);
            Name name = datum.Datatype.Components[0];
            Assert.NotNull(name);
            Assert.Single(name.Words);
            Assert.Equal("text", name.Words[0]);
        }
    }

    [Fact(DisplayName = "empty parenthesis")]
    public void Empty()
    {
        // ()

        Tokens tokens = new();
        tokens.Add<OpenParenthesis>()
            .Add<CloseParenthesis>();

        Parser parser = new(tokens.ToArray());
        var arguments = Parameters.Parse(ref parser);

        Assert.NotNull(arguments);
        Assert.Empty(arguments.Values);
    }
}
