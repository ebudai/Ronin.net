using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Punctuation;

namespace Unit;

[Trait("Parser", null)]
public class Arguments
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
        var arguments = Ronin.Grammar.Compound.Arguments.Parse(ref parser);

        Assert.Single(arguments?.Values);
        Ronin.Grammar.Reference reference = arguments.Values[0];
        Assert.Single(reference?.Components);
        Ronin.Grammar.Name name = reference.Components[0];
        Assert.Equal(1, name?.Source.Length);
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
        var arguments = Ronin.Grammar.Compound.Arguments.Parse(ref parser);

        Assert.Equal(2, arguments?.Values?.Count);

        {
            Ronin.Grammar.Reference reference = arguments.Values[0];
            Assert.Single(reference?.Components);
            Ronin.Grammar.Name name = reference.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            Ronin.Grammar.Reference reference = arguments.Values[1];
            Assert.Single(reference?.Components);
            Ronin.Grammar.Name name = reference.Components[0];
            Assert.Equal(1, name?.Source.Length);
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
        var arguments = Ronin.Grammar.Compound.Arguments.Parse(ref parser);
        Assert.Empty(arguments?.Values);
    }

    [Fact(DisplayName = "named")]
    public void Named()
    {
        // (1, 2, thing)

        Token[] tokens =
        {
            new OpenParenthesis(),
            new NumberLiteral(),
            new Separator(),
            new NumberLiteral(),
            new Separator(),
            new Word(),
            new CloseParenthesis(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Arguments.Parse(ref parser);

        Assert.Equal(3, arguments?.Values?.Count);
        
        {
            Ronin.Grammar.Literal scalar = arguments.Values[0];
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            Ronin.Grammar.Literal scalar = arguments.Values[1];
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            Ronin.Grammar.Reference reference = arguments.Values[2];
            Assert.Single(reference?.Components);
            Ronin.Grammar.Name name = reference.Components[0];
            Assert.Equal(1, name?.Source.Length);
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
            new NumberLiteral(),
            new Separator(),
            new OpenParenthesis(),
            new NumberLiteral(),
            new Separator(),
            new NumberLiteral(),
            new Separator(),
            new NumberLiteral(),
            new CloseParenthesis(),
            new CloseParenthesis(),
            Sentinel.Instance,
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Arguments.Parse(ref parser);

        Assert.Equal(3, arguments?.Values?.Count);

        {
            Ronin.Grammar.Reference reference = arguments.Values[0];
            Assert.Single(reference?.Components);
            Ronin.Grammar.Name name = reference.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            Ronin.Grammar.Literal scalar = arguments.Values[1];
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            Ronin.Grammar.Compound.Arguments subargs = arguments.Values[2];
            Assert.Equal(3, subargs?.Values?.Count);

            {
                Ronin.Grammar.Literal scalar = subargs?.Values[0];
                Assert.Equal(1, scalar?.Source.Length);
            }

            {
                Ronin.Grammar.Literal scalar = subargs?.Values[1];
                Assert.Equal(1, scalar?.Source.Length);
            }

            {
                Ronin.Grammar.Literal scalar = subargs?.Values[2];
                Assert.Equal(1, scalar?.Source.Length);
            }
        }
    }
}
