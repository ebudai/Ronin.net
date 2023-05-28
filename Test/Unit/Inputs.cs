using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Unit;

[Trait("Parser", null)]
public class Inputs
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // (stuff)

        Token[] tokens =
        {
            new StartValues(),
            new Word(),
            new EndValues(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Inputs.Parse(ref parser);

        Assert.Single(arguments?.Values);
        var reference = arguments.Values[0] as Ronin.Grammar.Reference;
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
            new StartValues(),
            new Word(),
            new Separator(),
            new Word(),
            new EndValues(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Inputs.Parse(ref parser);

        Assert.Equal(2, arguments?.Values?.Count);

        {
            var reference = arguments.Values[0] as Ronin.Grammar.Reference;
            Assert.Single(reference?.Components);
            Ronin.Grammar.Name name = reference.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            var reference = arguments.Values[1] as Ronin.Grammar.Reference;
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
            new StartValues(),
            new EndValues(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Inputs.Parse(ref parser);
        Assert.Empty(arguments?.Values);
    }

    [Fact(DisplayName = "named")]
    public void Named()
    {
        // (1, 2, thing)

        Token[] tokens =
        {
            new StartValues(),
            new NumberLiteral(),
            new Separator(),
            new NumberLiteral(),
            new Separator(),
            new Word(),
            new EndValues(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Inputs.Parse(ref parser);

        Assert.Equal(3, arguments?.Values?.Count);
        
        {
            var scalar = arguments.Values[0] as Ronin.Grammar.Literal;
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            var scalar = arguments.Values[1] as Ronin.Grammar.Literal;
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            var reference = arguments.Values[2] as Ronin.Grammar.Reference;
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
            new StartValues(),
            new Word(),
            new Separator(),
            new NumberLiteral(),
            new Separator(),
            new StartValues(),
            new NumberLiteral(),
            new Separator(),
            new NumberLiteral(),
            new Separator(),
            new NumberLiteral(),
            new EndValues(),
            new EndValues(),
            Sentinel.Instance,
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Inputs.Parse(ref parser);

        Assert.Equal(3, arguments?.Values?.Count);

        {
            var reference = arguments.Values[0] as Ronin.Grammar.Reference;
            Assert.Single(reference?.Components);
            Ronin.Grammar.Name name = reference.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            var scalar = arguments.Values[1] as Ronin.Grammar.Literal;
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            var subargs = arguments.Values[2] as Ronin.Grammar.Compound.Inputs;
            Assert.Equal(3, subargs?.Values?.Count);

            {
                var scalar = subargs?.Values[0] as Ronin.Grammar.Literal;
                Assert.Equal(1, scalar?.Source.Length);
            }

            {
                var scalar = subargs?.Values[1] as Ronin.Grammar.Literal;
                Assert.Equal(1, scalar?.Source.Length);
            }

            {
                var scalar = subargs?.Values[2] as Ronin.Grammar.Literal;
                Assert.Equal(1, scalar?.Source.Length);
            }
        }
    }
}
