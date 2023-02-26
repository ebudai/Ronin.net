using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

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
            new OpenParenthesisSymbol(),
            new Word(),
            new CloseParenthesisSymbol(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Aggregates.Arguments.Parse(ref parser);

        Assert.Single(arguments?.Values);
        Ronin.Grammar.Reference reference = arguments.Values[0];
        Assert.Single(reference?.Components);
        Ronin.Grammar.Name name = reference.Components[0];
        Assert.Single(name?.Source);
    }

    [Fact(DisplayName = "multiple")]
    public void Multiple()
    {
        // (test, stuff)

        Token[] tokens =
        {
            new OpenParenthesisSymbol(),
            new Word(),
            new SeparatorSymbol(),
            new Word(),
            new CloseParenthesisSymbol(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Aggregates.Arguments.Parse(ref parser);

        Assert.Equal(2, arguments?.Values?.Count);

        {
            Ronin.Grammar.Reference reference = arguments.Values[0];
            Assert.Single(reference?.Components);
            Ronin.Grammar.Name name = reference.Components[0];
            Assert.Single(name?.Source);
        }

        {
            Ronin.Grammar.Reference reference = arguments.Values[1];
            Assert.Single(reference?.Components);
            Ronin.Grammar.Name name = reference.Components[0];
            Assert.Single(name?.Source);
        }
    }

    [Fact(DisplayName = "empty parenthesis")]
    public void Empty()
    {
        // ()

        Token[] tokens =
        {
            new OpenParenthesisSymbol(),
            new CloseParenthesisSymbol(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Aggregates.Arguments.Parse(ref parser);
        Assert.Empty(arguments?.Values);
    }

    [Fact(DisplayName = "named")]
    public void Named()
    {
        // (1, 2, thing)

        Token[] tokens =
        {
            new OpenParenthesisSymbol(),
            new NumberLiteral(),
            new SeparatorSymbol(),
            new NumberLiteral(),
            new SeparatorSymbol(),
            new Word(),
            new CloseParenthesisSymbol(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Aggregates.Arguments.Parse(ref parser);

        Assert.Equal(3, arguments?.Values?.Count);
        
        {
            LiteralSyntax scalar = arguments.Values[0];
            Assert.Single(scalar?.Source);
        }

        {
            LiteralSyntax scalar = arguments.Values[1];
            Assert.Single(scalar?.Source);
        }

        {
            Ronin.Grammar.Reference reference = arguments.Values[2];
            Assert.Single(reference?.Components);
            Ronin.Grammar.Name name = reference.Components[0];
            Assert.Single(name?.Source);
        }
    }

    [Fact(DisplayName = "arguments of arguments")]
    public void Recursive()
    {
        // (a, 3, (1, 2, 3))

        Token[] tokens =
        {
            new OpenParenthesisSymbol(),
            new Word(),
            new SeparatorSymbol(),
            new NumberLiteral(),
            new SeparatorSymbol(),
            new OpenParenthesisSymbol(),
            new NumberLiteral(),
            new SeparatorSymbol(),
            new NumberLiteral(),
            new SeparatorSymbol(),
            new NumberLiteral(),
            new CloseParenthesisSymbol(),
            new CloseParenthesisSymbol(),
            Sentinel.Instance,
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Aggregates.Arguments.Parse(ref parser);

        Assert.Equal(3, arguments?.Values?.Count);

        {
            Ronin.Grammar.Reference reference = arguments.Values[0];
            Assert.Single(reference?.Components);
            Ronin.Grammar.Name name = reference.Components[0];
            Assert.Single(name?.Source);
        }

        {
            LiteralSyntax scalar = arguments.Values[1];
            Assert.Single(scalar?.Source);
        }

        {
            Ronin.Grammar.Aggregates.Arguments subargs = arguments.Values[2];
            Assert.Equal(3, subargs?.Values?.Count);

            {
                LiteralSyntax scalar = subargs?.Values[0];
                Assert.Single(scalar?.Source);
            }

            {
                LiteralSyntax scalar = subargs?.Values[1];
                Assert.Single(scalar?.Source);
            }

            {
                LiteralSyntax scalar = subargs?.Values[2];
                Assert.Single(scalar?.Source);
            }
        }
    }
}
