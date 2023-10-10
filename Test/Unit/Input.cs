using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;
using Literal = Ronin.Grammar.Literal;

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
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var inputs = Inputs.Parse(ref parser);

        Assert.Single(inputs);
        var member = inputs[0].AsT1 as Member.Unresolved;
        Assert.Single(member?.Reference);
        var name = member.Reference.Span[0].AsT0;
        Assert.Single(name?.Tokens.ToArray());
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
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var arguments = Inputs.Parse(ref parser);

        Assert.Equal(2, arguments?.Count);

        {
            var member = arguments[0].AsT1 as Member.Unresolved;
            Assert.Single(member?.Reference);
            var name = member.Reference.Span[0].AsT0;
            Assert.Single(name?.Tokens.ToArray());
        }

        {
            var member = arguments[1].AsT1 as Member.Unresolved;
            Assert.Single(member?.Reference);
            var name = member.Reference.Span[0].AsT0;
            Assert.Single(name?.Tokens.ToArray());
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
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var arguments = Inputs.Parse(ref parser);
        Assert.Null(arguments);
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
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var arguments = Inputs.Parse(ref parser);

        Assert.Equal(3, arguments?.Count);
        
        {
            var scalar = arguments[0].AsT1 as Literal;
            Assert.Single(scalar?.Tokens.ToArray());
        }

        {
            var scalar = arguments[1].AsT1 as Literal;
            Assert.Single(scalar?.Tokens.ToArray());
        }

        {
            var member = arguments[2].AsT1 as Member.Unresolved;
            Assert.Single(member?.Reference);
            var name = member.Reference.Span[0].AsT0;
            Assert.Single(name?.Tokens.ToArray());
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
            new Sentinel(),
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var arguments = Inputs.Parse(ref parser);

        Assert.Equal(3, arguments?.Count);

        {
            var member = arguments[0].AsT1 as Member.Unresolved;
            Assert.Single(member?.Reference);
            var name = member.Reference.Span[0].AsT0;
            Assert.Single(name?.Tokens.ToArray());
        }

        {
            var scalar = arguments[1].AsT1 as Literal;
            Assert.Single(scalar?.Tokens.ToArray());
        }

        {
            var subargs = arguments[2].AsT1 as Inputs;
            Assert.Equal(3, subargs?.Count);

            {
                var scalar = subargs[0].AsT1 as Literal;
                Assert.Single(scalar?.Tokens.ToArray());
            }

            {
                var scalar = subargs[1].AsT1 as Literal;
                Assert.Single(scalar?.Tokens.ToArray());
            }

            {
                var scalar = subargs[2].AsT1 as Literal;
                Assert.Single(scalar?.Tokens.ToArray());
            }
        }
    }

    [Fact(DisplayName = "default value")]
    public void DefaultValue()
    {
        const string variable = nameof(variable);

        // (3, variable = 7)

        List<Token> tokens = new()
        {
            StartValues(),
            Number(3),
            Separator(),
            Word(variable),
            Assign(),
            Number(7),
            EndValues(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var arguments = Inputs.Parse(ref parser);

        Assert.Equal(2, arguments?.Count);

        Assert.True(arguments[1].IsT0);
        Association assignment = arguments[1].AsT0;
        var member = assignment.Destination as Member.Unresolved;
        Assert.Single(member?.Reference);
        Assert.True(member.Reference.Span[0].IsT0);
        var name = member.Reference.Span[0].AsT0;
        Assert.Single(name.Tokens.ToArray());
        Assert.Equal(variable, name.Tokens.Span[0].Memory.ToString());
    }
}
