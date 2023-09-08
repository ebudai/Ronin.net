using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Input : ParsingTests
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
        var arguments = Inputs.Parse(ref parser);

        Assert.Single(arguments);
        Value value = arguments[0];
        var member = value as Context.Member.Unresolved;
        Assert.Single(member?.Reference?.Components);
        Name name = member.Reference.Components[0];
        Assert.Single(name?.Source.ToArray());
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
        var arguments = Inputs.Parse(ref parser);

        Assert.Equal(2, arguments?.Count);

        {
            Value value = arguments[0];
            var member = value as Context.Member.Unresolved;
            Assert.Single(member?.Reference?.Components);
            Name name = member.Reference.Components[0];
            Assert.Single(name?.Source.ToArray());
        }

        {
            Value value = arguments[1];
            var member = value as Context.Member.Unresolved;
            Assert.Single(member?.Reference?.Components);
            Name name = member.Reference.Components[0];
            Assert.Single(name?.Source.ToArray());
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
        var arguments = Inputs.Parse(ref parser);
        Assert.Empty(arguments);
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
        var arguments = Inputs.Parse(ref parser);

        Assert.Equal(3, arguments?.Count);
        
        {
            Value value = arguments[0];
            var scalar = value as Inline;
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            Value value = arguments[1];
            var scalar = value as Inline;
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            Value value = arguments[2];
            var member = value as Context.Member.Unresolved;
            Assert.Single(member?.Reference?.Components);
            Name name = member.Reference.Components[0];
            Assert.Single(name?.Source.ToArray());
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
        var arguments = Inputs.Parse(ref parser);

        Assert.Equal(3, arguments?.Count);

        {
            Value value = arguments[0];
            var member = value as Context.Member.Unresolved;
            Assert.Single(member?.Reference?.Components);
            Name name = member.Reference.Components[0];
            Assert.Single(name?.Source.ToArray());
        }

        {
            Value value = arguments[1];
            var scalar = value as Inline;
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            Value value = arguments[2];
            var subargs = value as Inputs;
            Assert.Equal(3, subargs?.Count);

            {
                Value subvalue = subargs[0];
                var scalar = subvalue as Inline;
                Assert.Equal(1, scalar?.Source.Length);
            }

            {
                Value subvalue = subargs[1];
                var scalar = subvalue as Inline;
                Assert.Equal(1, scalar?.Source.Length);
            }

            {
                Value subvalue = subargs[2];
                var scalar = subvalue as Inline;
                Assert.Equal(1, scalar?.Source.Length);
            }
        }
    }
}
