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
public class arguments
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // (stuff)

        Tokens tokens = new();
        tokens.Add<OpenParenthesis>()
            .Add<Word>("stuff")
            .Add<CloseParenthesis>();

        Parser parser = new(tokens.ToArray());
        var arguments = Arguments.Parse(ref parser);

        Reference reference = arguments?.Values?[0];
        Name name = reference?.Components?[0];
        Assert.Equal("stuff", name?.Words?[0]);
    }

    [Fact(DisplayName = "multiple")]
    public void Multiple()
    {
        // (test, stuff)

        Tokens tokens = new();
        tokens.Add<OpenParenthesis>()
            .Add<Word>("test")
            .Add<Separator>()
            .Add<Word>("stuff")
            .Add<CloseParenthesis>();

        Parser parser = new(tokens.ToArray());
        var arguments = Arguments.Parse(ref parser);

        {
            Reference reference = arguments?.Values?[0];
            Name name = reference?.Components?[0];
            Assert.Equal("test", name?.Words?[0]);
        }

        {
            Reference reference = arguments?.Values?[1];
            Name name = reference?.Components?[0];
            Assert.Equal("stuff", name?.Words?[0]);
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
        var arguments = Arguments.Parse(ref parser);
        Assert.Empty(arguments?.Values);
    }

    [Fact(DisplayName = "named")]
    public void Named()
    {
        // (1, 2, thing)

        Tokens tokens = new();
        tokens.Add<OpenParenthesis>()
            .Add<Number>("1")
            .Add<Separator>()
            .Add<Number>("2")
            .Add<Separator>()
            .Add<Word>("thing")
            .Add<CloseParenthesis>();

        Parser parser = new(tokens.ToArray());
        var arguments = Arguments.Parse(ref parser);

        {
            Temporary temporary = arguments?.Values?[0];
            Scalar scalar = temporary;
            Assert.Equal("1", scalar?.Literals?[0]?.ToString());
        }

        {
            Temporary temporary = arguments?.Values?[1];
            Scalar scalar = temporary;
            Assert.Equal("2", scalar?.Literals?[0]?.ToString());
        }

        Reference reference = arguments?.Values?[2];
        Name name = reference?.Components?[0];
        Assert.Equal("thing", name?.Words?[0]);
    }
}
