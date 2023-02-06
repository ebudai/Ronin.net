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

        Assert.Single(arguments?.Values);
        Reference reference = arguments.Values[0];
        Assert.Single(reference?.Components);
        Name name = reference.Components[0];
        Assert.Single(name?.Words);
        Assert.Equal("stuff", name.Words[0]);
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

        Assert.Equal(2, arguments?.Values?.Count);

        {            
            Reference reference = arguments.Values[0];
            Assert.Single(reference?.Components);
            Name name = reference.Components[0];
            Assert.Single(name?.Words);
            Assert.Equal("test", name.Words[0]);
        }

        {
            Reference reference = arguments.Values[1];
            Assert.Single(reference?.Components);
            Name name = reference.Components[0];
            Assert.Single(name?.Words);
            Assert.Equal("stuff", name.Words[0]);
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

        Assert.Equal(3, arguments?.Values?.Count);
        
        {
            Scalar scalar = arguments.Values[0];
            Assert.Single(scalar?.Literals);
            Assert.Equal("1", scalar.Literals[0]?.ToString());
        }

        {
            Scalar scalar = arguments.Values[1];
            Assert.Single(scalar?.Literals);
            Assert.Equal("2", scalar.Literals[0]?.ToString());
        }

        {
            Reference reference = arguments.Values[2];
            Assert.Single(reference?.Components);
            Name name = reference.Components[0];
            Assert.Single(name?.Words);
            Assert.Equal("thing", name.Words[0]);
        }
    }

    [Fact(DisplayName = "arguments of arguments")]
    public void Recursive()
    {
        // (a, 3, (1, 2, 3))

        Tokens tokens = new();
        tokens.Add<OpenParenthesis>()
            .Add<Word>("a")
            .Add<Separator>()
            .Add<Number>("3")
            .Add<Separator>()
            .Add<OpenParenthesis>()
            .Add<Number>("1")
            .Add<Separator>()
            .Add<Number>("2")
            .Add<Separator>()
            .Add<Number>("3")
            .Add<CloseParenthesis>()
            .Add<CloseParenthesis>();

        Parser parser = new(tokens.ToArray());
        var arguments = Arguments.Parse(ref parser);

        Assert.Equal(3, arguments?.Values?.Count);

        {
            Reference reference = arguments.Values[0];
            Assert.Single(reference?.Components);
            Name name = reference.Components[0];
            Assert.Single(name?.Words);
            Assert.Equal("a", name.Words[0]);
        }

        {
            Scalar scalar = arguments.Values[1];
            Assert.Single(scalar?.Literals);
            Assert.Equal("3", scalar.Literals[0]?.ToString());
        }

        {
            Arguments subargs = arguments.Values[2];
            Assert.Equal(3, subargs?.Values?.Count);

            {
                Scalar scalar = subargs?.Values[0];
                Assert.Single(scalar?.Literals);
                Assert.Equal("1", scalar.Literals[0]?.ToString());
            }

            {
                Scalar scalar = subargs?.Values[1];
                Assert.Single(scalar?.Literals);
                Assert.Equal("2", scalar.Literals[0]?.ToString());
            }

            {
                Scalar scalar = subargs?.Values[2];
                Assert.Single(scalar?.Literals);
                Assert.Equal("3", scalar.Literals[0]?.ToString());
            }
        }
    }
}
