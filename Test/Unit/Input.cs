using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;
using Literal = Ronin.Grammar.Literal;

namespace Unit;

[Trait(nameof(Parser), null)]
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
        var member = inputs[0].AsValue as Member.Unresolved;
        Assert.Single(member?.Reference);
        var name = member.Reference.Span[0].AsName;
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
            var member = arguments[0].AsValue as Member.Unresolved;
            Assert.Single(member?.Reference);
            var name = member.Reference.Span[0].AsName;
            Assert.Single(name?.Tokens.ToArray());
        }

        {
            var member = arguments[1].AsValue as Member.Unresolved;
            Assert.Single(member?.Reference);
            var name = member.Reference.Span[0].AsName;
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
        
        Assert.NotNull(arguments);
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
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var arguments = Inputs.Parse(ref parser);

        Assert.Equal(3, arguments?.Count);
        
        {
            var scalar = arguments[0].AsValue as Literal;
            Assert.Single(scalar?.Tokens.ToArray());
        }

        {
            var scalar = arguments[1].AsValue as Literal;
            Assert.Single(scalar?.Tokens.ToArray());
        }

        {
            var member = arguments[2].AsValue as Member.Unresolved;
            Assert.Single(member?.Reference);
            var name = member.Reference.Span[0].AsName;
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
            var member = arguments[0].AsValue as Member.Unresolved;
            Assert.Single(member?.Reference);
            var name = member.Reference.Span[0].AsName;
            Assert.Single(name?.Tokens.ToArray());
        }

        {
            var scalar = arguments[1].AsValue as Literal;
            Assert.Single(scalar?.Tokens.ToArray());
        }

        {
            var subargs = arguments[2].AsValue as Inputs;
            Assert.Equal(3, subargs?.Count);

            {
                var scalar = subargs[0].AsValue as Literal;
                Assert.Single(scalar?.Tokens.ToArray());
            }

            {
                var scalar = subargs[1].AsValue as Literal;
                Assert.Single(scalar?.Tokens.ToArray());
            }

            {
                var scalar = subargs[2].AsValue as Literal;
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

        var assignment = arguments[1].AsAssociation;
        var member = assignment?.Destination as Member.Unresolved;
        Assert.Single(member?.Reference);
        var name = member.Reference.Span[0].AsName;
        Assert.Single(name.Tokens.ToArray());
        Assert.Equal(variable, name.Tokens.Span[0].Memory.ToString());
    }
}
