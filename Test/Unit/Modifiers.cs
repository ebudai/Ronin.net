using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait(nameof(Parser), null)]
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
            Arrow(),
            Keyword.Compiled(),
            Word("money"),
            Terminal(),
        };

        Parser parser = new(tokens.AsLinkedList());
        var datum = Datum.Parse(ref parser);

        Assert.True(datum.Modifiers.Is<Compiled>());
        Assert.False(datum.Modifiers.Is<Global>());

    }

    [Fact(DisplayName = $"{Global.keyword}")]
    public void IsShared()
    {
        // var x => shared money;

        List<Token> tokens = new()
        {
            Keyword.Variable(),
            Word("x"),
            Arrow(),
            Keyword.Shared(),
            Word("money"),
            Terminal(),
        };

        Parser parser = new(tokens.AsLinkedList());
        var datum = Datum.Parse(ref parser);

        Assert.True(datum.Modifiers.Is<Global>());
        Assert.False(datum.Modifiers.Is<Compiled>());

    }
}
