using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Parameter : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // (var test => money)

        List<Token> tokens = new()
        {
            StartValues(),
            Keyword.Variable(),
            Word("test"),
            Returns(),
            Word("money"),
            EndValues(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var parameters = Parameters.Parse(ref parser);

        Assert.Single(parameters);

        var datum = parameters[0].AsT0;

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.False(datum.Modifiers.Is<Compiled>());
        Assert.False(datum.Modifiers.Is<Global>());
        Assert.False(datum.Modifiers.Is<Optional>());

        Assert.Single(datum.Identifier);

        Assert.Single(datum.Type?.Identifier);
        var name = datum.Type.Identifier.Components[0].AsT0;
        Assert.Single(name?.Tokens.ToArray());
    }

    [Fact(DisplayName = "multiple")]
    public void Multiple()
    {
        // (test => number, stuff in things => text)

        List<Token> tokens = new()
        {
            StartValues(),
            Word("test"),
            Returns(),
            Word("number"),
            Separator(),
            Word("stuff"),
            Word("in"),
            Word("things"),
            Returns(),
            Word("text"),
            EndValues(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var parameters = Parameters.Parse(ref parser);

        Assert.Equal(2, parameters?.Count);

        {
            Datum datum = parameters[0].AsT0;
            
            Assert.Null(datum?.Mutability);

            Assert.False(datum.Modifiers.Is<Compiled>());
            Assert.False(datum.Modifiers.Is<Global>());
            Assert.False(datum.Modifiers.Is<Optional>());

            Assert.Single(datum.Identifier);
        
            Assert.Single(datum.Type?.Identifier);
            var name = datum.Type.Identifier.Components[0].AsT0;
            Assert.Single(name?.Tokens.ToArray());
        }

        {
            var datum = parameters[1].AsT0;

            Assert.Null(datum?.Mutability);

            Assert.False(datum.Modifiers.Is<Compiled>());
            Assert.False(datum.Modifiers.Is<Global>());
            Assert.False(datum.Modifiers.Is<Optional>());

            Assert.Single(datum.Type.Identifier);
            var name = datum.Type.Identifier.Components[0].AsT0;
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
            Sentinel.Instance
        };

        Parser parser = new(tokens.AsLinkedList());
        var arguments = Parameters.Parse(ref parser);

        Assert.Empty(arguments);
    }
}
