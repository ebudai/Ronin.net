using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Inputs : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // (stuff)

        List<Token> tokens = new()
        {
            StartValues(),
            Word("stuff"),
            EndValues(),
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

        List<Token> tokens = new()
        {
            StartValues(),
            Word("test"),
            Separator(),
            Word("stuff"),
            EndValues(),
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

        List<Token> tokens = new()
        {
            StartValues(),
            EndValues(),
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

        List<Token> tokens = new()
        {
            StartValues(),
            Number(1),
            Separator(),
            Number(2),
            Separator(),
            Word("thing"),
            EndValues(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Inputs.Parse(ref parser);

        Assert.Equal(3, arguments?.Values?.Count);
        
        {
            Value value = arguments.Values[0];
            var scalar = value as Ronin.Grammar.Inline;
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            Value value = arguments.Values[1];
            var scalar = value as Ronin.Grammar.Inline;
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

        List<Token> tokens = new()
        {
            StartValues(),
            Word("a"),
            Separator(),
            Number(3),
            Separator(),
            StartValues(),
            Number(1),
            Separator(),
            Number(2),
            Separator(),
            Number(3),
            EndValues(),
            EndValues(),
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
            var scalar = value as Ronin.Grammar.Inline;
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            Value value = arguments.Values[2];
            var subargs = value as Ronin.Grammar.Compound.Inputs;
            Assert.Equal(3, subargs?.Values?.Count);

            {
                Value subvalue = subargs?.Values[0];
                var scalar = subvalue as Ronin.Grammar.Inline;
                Assert.Equal(1, scalar?.Source.Length);
            }

            {
                Value subvalue = subargs?.Values[1];
                var scalar = subvalue as Ronin.Grammar.Inline;
                Assert.Equal(1, scalar?.Source.Length);
            }

            {
                Value subvalue = subargs?.Values[2];
                var scalar = subvalue as Ronin.Grammar.Inline;
                Assert.Equal(1, scalar?.Source.Length);
            }
        }
    }
}
