using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Parameters : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // (var test => money)

        List<Token> tokens = new()
        {
            StartValues(),
            Variable(),
            Word("test"),
            Returns(),
            Word("money"),
            EndValues(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var parameters = Ronin.Grammar.Compound.Parameters.Parse(ref parser);

        Assert.Single(parameters?.Values);

        Ronin.Grammar.Datum.Declaration datum = parameters.Values[0];

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.False(datum.Modifiers.Is<Compiled>());
        Assert.False(datum.Modifiers.Is<Shared>());
        Assert.False(datum.Modifiers.Is<Optional>());
        Assert.False(datum.Modifiers.Is<Persistent>());

        Assert.Equal(1, datum.Name?.Source.Length);

        Assert.Single(datum.Datatype?.Components);
        Ronin.Grammar.Name name = datum.Datatype.Components[0];
        Assert.Equal(1, name?.Source.Length);
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
        
        Parser parser = new(tokens);
        var parameters = Ronin.Grammar.Compound.Parameters.Parse(ref parser);

        Assert.Equal(2, parameters?.Values?.Count);

        {
            Ronin.Grammar.Datum.Declaration datum = parameters.Values[0];
            
            Assert.Null(datum?.Mutability);

            Assert.False(datum.Modifiers.Is<Compiled>());
            Assert.False(datum.Modifiers.Is<Shared>());
            Assert.False(datum.Modifiers.Is<Optional>());
            Assert.False(datum.Modifiers.Is<Persistent>());

            Assert.Equal(1, datum.Name?.Source.Length);
        
            Assert.Single(datum.Datatype?.Components);
            Ronin.Grammar.Name name = datum.Datatype.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            Ronin.Grammar.Datum.Declaration datum = parameters.Values[1];

            Assert.Null(datum?.Mutability);

            Assert.False(datum.Modifiers.Is<Compiled>());
            Assert.False(datum.Modifiers.Is<Shared>());
            Assert.False(datum.Modifiers.Is<Optional>());
            Assert.False(datum.Modifiers.Is<Persistent>());

            Assert.Single(datum.Datatype?.Components);
            Ronin.Grammar.Name name = datum.Datatype.Components[0];
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
        var arguments = Ronin.Grammar.Compound.Parameters.Parse(ref parser);

        Assert.Empty(arguments?.Values);
    }
}
