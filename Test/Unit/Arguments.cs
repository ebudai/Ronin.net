using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;

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

        Token[] tokens =
        {
            new OpenParenthesis(),
            new Word(),
            new CloseParenthesis(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var arguments = Arguments.Parse(ref parser);

        Assert.Single(arguments?.Values);
        Reference reference = arguments.Values[0];
        Assert.Single(reference?.Components);
        Name name = reference.Components[0];
        Assert.Single(name?.Source);
    }

    [Fact(DisplayName = "multiple")]
    public void Multiple()
    {
        // (test, stuff)

        Token[] tokens =
        {
            new OpenParenthesis(),
            new Word(),
            new Separator(),
            new Word(),
            new CloseParenthesis(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var arguments = Arguments.Parse(ref parser);

        Assert.Equal(2, arguments?.Values?.Count);

        {            
            Reference reference = arguments.Values[0];
            Assert.Single(reference?.Components);
            Name name = reference.Components[0];
            Assert.Single(name?.Source);
        }

        {
            Reference reference = arguments.Values[1];
            Assert.Single(reference?.Components);
            Name name = reference.Components[0];
            Assert.Single(name?.Source);
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
        var arguments = Arguments.Parse(ref parser);
        Assert.Empty(arguments?.Values);
    }

    [Fact(DisplayName = "named")]
    public void Named()
    {
        // (1, 2, thing)

        Token[] tokens =
        {
            new OpenParenthesis(),
            new Number(),
            new Separator(),
            new Number(),
            new Separator(),
            new Word(),
            new CloseParenthesis(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var arguments = Arguments.Parse(ref parser);

        Assert.Equal(3, arguments?.Values?.Count);
        
        {
            Scalar scalar = arguments.Values[0];
            Assert.Single(scalar?.Source);
        }

        {
            Scalar scalar = arguments.Values[1];
            Assert.Single(scalar?.Source);
        }

        {
            Reference reference = arguments.Values[2];
            Assert.Single(reference?.Components);
            Name name = reference.Components[0];
            Assert.Single(name?.Source);
        }
    }

    [Fact(DisplayName = "arguments of arguments")]
    public void Recursive()
    {
        // (a, 3, (1, 2, 3))

        Token[] tokens =
        {
            new OpenParenthesis(),
            new Word(),
            new Separator(),
            new Number(),
            new Separator(),
            new OpenParenthesis(),
            new Number(),
            new Separator(),
            new Number(),
            new Separator(),
            new Number(),
            new CloseParenthesis(),
            new CloseParenthesis(),
            Sentinel.Instance,
        };
        
        Parser parser = new(tokens);
        var arguments = Arguments.Parse(ref parser);

        Assert.Equal(3, arguments?.Values?.Count);

        {
            Reference reference = arguments.Values[0];
            Assert.Single(reference?.Components);
            Name name = reference.Components[0];
            Assert.Single(name?.Source);
        }

        {
            Scalar scalar = arguments.Values[1];
            Assert.Single(scalar?.Source);
        }

        {
            Arguments subargs = arguments.Values[2];
            Assert.Equal(3, subargs?.Values?.Count);

            {
                Scalar scalar = subargs?.Values[0];
                Assert.Single(scalar?.Source);
            }

            {
                Scalar scalar = subargs?.Values[1];
                Assert.Single(scalar?.Source);
            }

            {
                Scalar scalar = subargs?.Values[2];
                Assert.Single(scalar?.Source);
            }
        }
    }
}
