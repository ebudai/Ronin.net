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
public class ordinal
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // [test]

        Tokens tokens = new();
        tokens.Add<OpenSquareBracket>()
            .Add<Word>("test")
            .Add<CloseSquareBracket>();

        Parser parser = new(tokens.ToArray());
        var arguments = Ordinal.Parse(ref parser);

        Assert.NotNull(arguments);

        Assert.Single(arguments.Values);
        Reference reference = arguments.Values[0];
        Assert.NotNull(reference);

        Assert.Single(reference.Components);
        Name name = reference.Components[0];
        Assert.NotNull(name);
        Assert.Single(name.Words);
        Assert.Equal("test", name.Words[0]);
    }

    [Fact(DisplayName = "multidimensional")]
    public void Multidimensional()
    {
        // [test, stuff]

        Tokens tokens = new();
        tokens.Add<OpenSquareBracket>()
            .Add<Word>("test")
            .Add<Separator>()
            .Add<Word>("stuff")
            .Add<CloseSquareBracket>();

        Parser parser = new(tokens.ToArray());
        var arguments = Ordinal.Parse(ref parser);

        Assert.NotNull(arguments);
        Assert.NotNull(arguments.Values);
        Assert.Equal(2, arguments.Values.Count);

        Reference test = arguments.Values[0];
        Assert.Single(test.Components);
        Name name = test.Components[0];
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
        // []

        Tokens tokens = new();
        tokens.Add<OpenSquareBracket>()
            .Add<CloseSquareBracket>();

        Parser parser = new(tokens.ToArray());
        var arguments = Ordinal.Parse(ref parser);

        Assert.NotNull(arguments);
        Assert.Empty(arguments.Values);
    }

    [Fact(DisplayName = "multidimensional named")]
    public void MultidimensionalNamed()
    {
        // [1, 2, thing]

        Tokens tokens = new();
        tokens.Add<OpenSquareBracket>()
            .Add<Number>("1")
            .Add<Separator>()
            .Add<Number>("2")
            .Add<Separator>()
            .Add<Word>("thing")
            .Add<CloseSquareBracket>();

        Parser parser = new(tokens.ToArray());
        var arguments = Ordinal.Parse(ref parser);

        Assert.NotNull(arguments);
        Assert.NotEmpty(arguments.Values);

        Temporary temporary = arguments.Values[0];
        Assert.NotNull(temporary);
        Scalar scalar = temporary;
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
        Name name = reference.Components[0];
        Assert.NotNull(name);
        Assert.NotEmpty(name.Words);
        Assert.Equal("thing", name.Words[0]);
    }
}
