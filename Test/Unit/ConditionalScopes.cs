using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait(nameof(Parser), null)]
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
            Symbol(">"),
            Number(3),
            StartScope(),
            Word("return"),
            Number(2),
            Terminal(),
            EndScope(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var conditional = Scope.Conditional.Parse(ref parser);

        Assert.NotNull(conditional?.Condition);
        Assert.NotNull(conditional?.Condition);
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
            new Sentinel(),
        };

        Parser parser = new(tokens.AsLinkedList());
        var conditional = Scope.Conditional.Parse(ref parser);

        Assert.True(conditional?.Modifiers.Is<Compiled>());
    }
}
