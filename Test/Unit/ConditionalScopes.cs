using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class ConditionalScopes : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // if x > 3 { return 2; }

        List<Token> tokens = new()
        {
            Keyword.If(),
            Word("x"),
            Word(">"),
            Number(3),
            StartScope(),
            Word("return"),
            Number(2),
            Terminal(),
            EndScope(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var conditional = ConditionalScope.Parse(ref parser);

        Assert.NotNull(conditional?.Condition);
        Assert.NotNull(conditional.Definition);
    }

    [Fact(DisplayName = "compiled")]
    public void Compiled()
    {
        // compiled if x is nothing { y = 4; }

        List<Token> tokens = new()
        {
            Keyword.Compiled(),
            Keyword.If(),
            Word("x"),
            Word("is"),
            Word("nothing"),
            StartScope(),
            Word("y"),
            Assign(),
            Number(4),
            Terminal(),
            EndScope(),
            Sentinel.Instance,
        };

        Parser parser = new(tokens);
        var conditional = ConditionalScope.Parse(ref parser);

        Assert.NotNull(conditional?.Modifiers);
        Assert.True(conditional.Modifiers.Is<Compiled>());        
    }
}
