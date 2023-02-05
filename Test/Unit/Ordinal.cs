using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
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
        var ordinal = Ordinal.Parse(ref parser);

        Reference reference = ordinal?.Values?[0];
        Name name = reference?.Components[0];
        Assert.Equal("test", name?.Words?[0]);
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
        var ordinal = Ordinal.Parse(ref parser);
        
        {
            Reference test = ordinal?.Values?[0];
            Name name = test?.Components?[0];
            Assert.Equal("test", name?.Words?[0]);
        }

        {
            Reference stuff = ordinal?.Values?[1];
            Name name = stuff?.Components?[0];
            Assert.Equal("stuff", name?.Words?[0]);
        }
    }

    [Fact(DisplayName = "empty parenthesis")]
    public void Empty()
    {
        // []

        Tokens tokens = new();
        tokens.Add<OpenSquareBracket>().Add<CloseSquareBracket>();

        Parser parser = new(tokens.ToArray());
        var ordinal = Ordinal.Parse(ref parser);

        Assert.Empty(ordinal?.Values);
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

        {
            Scalar scalar = arguments?.Values?[0];
            Assert.Equal("1", scalar?.Literals?[0]?.ToString());
        }

        {
            Scalar scalar = arguments?.Values?[1];
            Assert.Equal("2", scalar?.Literals?[0]?.ToString());
        }

        {
            Reference reference = arguments?.Values?[2];
            Name name = reference?.Components?[0];
            Assert.Equal("thing", name?.Words?[0]);
        }        
    }
}
