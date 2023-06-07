using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Literals;
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
        Value value = arguments.Values[0];
        var reference = value as Reference;
        Assert.Single(reference?.Components);
        Ronin.Grammar.Words name = reference.Components[0];
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
            Value value = arguments.Values[0];
            var reference = value as Reference;
            Assert.Single(reference?.Components);
            Ronin.Grammar.Words name = reference.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            Value value = arguments.Values[1];
            var reference = value as Reference;
            Assert.Single(reference?.Components);
            Ronin.Grammar.Words name = reference.Components[0];
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
            new Number(),
            new Separator(),
            new Number(),
            new Separator(),
            new Word(),
            new EndValues(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Inputs.Parse(ref parser);

        Assert.Equal(3, arguments?.Values?.Count);
        
        {
            Value value = arguments.Values[0];
            var scalar = value as Ronin.Grammar.Literal;
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            Value value = arguments.Values[1];
            var scalar = value as Ronin.Grammar.Literal;
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            Value value = arguments.Values[2];
            var reference = value as Reference;
            Assert.Single(reference?.Components);
            Ronin.Grammar.Words name = reference.Components[0];
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
            new Number(),
            new Separator(),
            new StartValues(),
            new Number(),
            new Separator(),
            new Number(),
            new Separator(),
            new Number(),
            new EndValues(),
            new EndValues(),
            Sentinel.Instance,
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Inputs.Parse(ref parser);

        Assert.Equal(3, arguments?.Values?.Count);

        {
            Value value = arguments.Values[0];
            var reference = value as Reference;
            Assert.Single(reference?.Components);
            Ronin.Grammar.Words name = reference.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            Value value = arguments.Values[1];
            var scalar = value as Ronin.Grammar.Literal;
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            Value value = arguments.Values[2];
            var subargs = value as Ronin.Grammar.Compound.Inputs;
            Assert.Equal(3, subargs?.Values?.Count);

            {
                Value subvalue = subargs?.Values[0];
                var scalar = subvalue as Ronin.Grammar.Literal;
                Assert.Equal(1, scalar?.Source.Length);
            }

            {
                Value subvalue = subargs?.Values[1];
                var scalar = subvalue as Ronin.Grammar.Literal;
                Assert.Equal(1, scalar?.Source.Length);
            }

            {
                Value subvalue = subargs?.Values[2];
                var scalar = subvalue as Ronin.Grammar.Literal;
                Assert.Equal(1, scalar?.Source.Length);
            }
        }
    }
}
