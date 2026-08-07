using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;
using Type = Ronin.Grammar.Type;

namespace Unit;

[Trait(nameof(Parser), null)]
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
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var parameters = Parameters.Parse(ref parser);

        Assert.Single(parameters);

        var datum = parameters[0].AsDatum;

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.False(datum.Modifiers.Is<Compiled>());
        Assert.False(datum.Modifiers.Is<Global>());

        Assert.Single(datum.Identifier);

        var unresolved = datum.Type as Type.Unresolved;
        Assert.Single(unresolved?.Reference);
        var name = unresolved.Reference.Span[0].AsName;
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
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var parameters = Parameters.Parse(ref parser);

        Assert.Equal(2, parameters?.Count);

        {            
            Datum datum = parameters[0].AsDatum;            
            
            Assert.NotNull(datum);
            Assert.Null(datum.Mutability);
            Assert.Empty(datum.Modifiers.Tokens.ToArray());
            Assert.Single(datum.Identifier);

            var unresolved = datum.Type as Type.Unresolved;
            Assert.Single(unresolved?.Reference);
            var name = unresolved.Reference.Span[0].AsName;
            Assert.Single(name?.Tokens.ToArray());
        }

        {
            var datum = parameters[1].AsDatum;

            Assert.NotNull(datum);
            Assert.Null(datum.Mutability);
            Assert.Empty(datum.Modifiers.Tokens.ToArray());
            Assert.Single(datum.Identifier);

            var unresolved = datum.Type as Type.Unresolved;
            Assert.Single(unresolved?.Reference);
            var name = unresolved.Reference.Span[0].AsName;
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
        var arguments = Parameters.Parse(ref parser);

        Assert.NotNull(arguments);
        Assert.Empty(arguments);
    }
}
