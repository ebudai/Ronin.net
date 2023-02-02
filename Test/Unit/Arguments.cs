using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Arguments
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        TokensGenerator tokens = new();
        tokens.Add<OpenParenthesis>()
            .Add<Ronin.Lexicon.Word>("test")
            .Add<CloseParenthesis, Terminal>();

        Parser parser = new(tokens.Tokens.ToArray());
        var arguments = Ronin.Grammar.Aggregates.Arguments.Parse(ref parser);

        Assert.NotNull(arguments);

        Assert.Single(arguments.Values);
        Reference reference = arguments.Values[0];
        Assert.NotNull(reference);

        Assert.Single(reference.Components);
        Ronin.Grammar.Name name = reference.Components[0];
        Assert.NotNull(name);
        Assert.Single(name.Words);
        Assert.Equal("test", name.Words[0]);
    }

    [Fact(DisplayName = "multiple")]
    public void Multiple()
    {
        TokensGenerator tokens = new();
        tokens.Add<OpenParenthesis>()
            .Add<Ronin.Lexicon.Word>("test")
            .Add<Separator, Ronin.Lexicon.Whitespace>()
            .Add<Ronin.Lexicon.Word>("stuff")
            .Add<CloseParenthesis, Terminal>();

        Parser parser = new(tokens.Tokens.ToArray());
        var arguments = Ronin.Grammar.Aggregates.Arguments.Parse(ref parser);

        Assert.NotNull(arguments);
        Assert.NotNull(arguments.Values);
        Assert.Equal(2, arguments.Values.Count);

        Reference test = arguments.Values[0];
        Assert.Single(test.Components);
        Ronin.Grammar.Name name = test.Components[0];
        Assert.NotNull(name);
        Assert.Single(name.Words);
        Assert.Equal("test", name.Words[0]);

        Reference stuff = arguments.Values[1];
        Assert.Single(stuff.Components);
        name = stuff.Components[0];
        Assert.NotNull(name);
        Assert.Single(name.Words);
        Assert.Equal("stuff", name.Words[0]);
    }

    [Fact(DisplayName = "empty parenthesis")]
    public void Empty()
    {
        TokensGenerator tokens = new();
        tokens.Add<OpenParenthesis, CloseParenthesis, Terminal>();

        Parser parser = new(tokens.Tokens.ToArray());
        var arguments = Ronin.Grammar.Aggregates.Arguments.Parse(ref parser);

        Assert.NotNull(arguments);
        Assert.Empty(arguments.Values);
    }

    [Fact(DisplayName = "named")]
    public void Named()
    {
        TokensGenerator tokens = new();
        tokens.Add<OpenParenthesis>()
            .Add<Number>("1")
            .Add<Separator, Ronin.Lexicon.Whitespace>()
            .Add<Number>("2")
            .Add<Separator, Ronin.Lexicon.Whitespace>()
            .Add<Ronin.Lexicon.Word>("thing")
            .Add<CloseParenthesis, Terminal>();

        Parser parser = new(tokens.Tokens.ToArray());
        var arguments = Ronin.Grammar.Aggregates.Arguments.Parse(ref parser);

        Assert.NotNull(arguments);
        Assert.NotEmpty(arguments.Values);

        Temporary temporary = arguments.Values[0];
        Assert.NotNull(temporary);
        Ronin.Grammar.Scalar scalar = temporary;
        Assert.NotNull(scalar);
        Assert.NotEmpty(scalar.Literals);
        Assert.Equal("1", scalar.Literals[0].ToString());

        temporary = arguments.Values[1];
        Assert.NotNull(temporary);
        scalar = temporary;
        Assert.NotNull(scalar);
        Assert.NotEmpty(scalar.Literals);
        Assert.Equal("2", scalar.Literals[0].ToString());

        Reference reference = arguments.Values[2];
        Assert.NotNull(reference);
        Assert.NotEmpty(reference.Components);
        Ronin.Grammar.Name name = reference.Components[0];
        Assert.NotNull(name);
        Assert.NotEmpty(name.Words);
        Assert.Equal("thing", name.Words[0]);
    }
}
