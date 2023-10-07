using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Modifiers : ParsingTests
{
    [Fact(DisplayName = $"{Compiled.keyword}")]
    public void IsCompiled()
    {
        // var x => compiled money;

        List<Token> tokens = new()
        {
            Keyword.Variable(),
            Word("x"),
            Returns(),
            Keyword.Compiled(),
            Word("money"),
            Terminal(),
        };

        Parser parser = new(tokens.AsLinkedList());
        var datum = Datum.Parse(ref parser);

        Assert.True(datum.Modifiers.Is<Compiled>());
        Assert.False(datum.Modifiers.Is<Global>());
        Assert.False(datum.Modifiers.Is<Optional>());
    }

    [Fact(DisplayName = $"{Global.keyword}")]
    public void IsShared()
    {
        // var x => shared money;

        List<Token> tokens = new()
        {
            Keyword.Variable(),
            Word("x"),
            Returns(),
            Keyword.Shared(),
            Word("money"),
            Terminal(),
        };

        Parser parser = new(tokens.AsLinkedList());
        var datum = Datum.Parse(ref parser);

        Assert.True(datum.Modifiers.Is<Global>());
        Assert.False(datum.Modifiers.Is<Compiled>());
        Assert.False(datum.Modifiers.Is<Optional>());
    }

    [Fact(DisplayName = $"{Optional.keyword}")]
    public void IsOptional()
    {
        // var x => optional money;

        List<Token> tokens = new()
        {
            Keyword.Variable(),
            Word("x"),
            Returns(),
            Keyword.Optional(),
            Word("money"),
            Terminal(),
        };

        Parser parser = new(tokens.AsLinkedList());
        var datum = Datum.Parse(ref parser);

        Assert.True(datum.Modifiers.Is<Optional>());
        Assert.False(datum.Modifiers.Is<Compiled>());
        Assert.False(datum.Modifiers.Is<Global>());
    }
}
