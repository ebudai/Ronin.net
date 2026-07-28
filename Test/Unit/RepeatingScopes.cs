using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait(nameof(Parser), null)]
public class RepeatingScopes : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // while x > 3 { return 2; }

        List<Token> tokens = new()
        {
            Keyword.While(),
            Word("x"),
            Word(">"),
            Number(3),
            StartScope(),
            Word("return"),
            Number(2),
            Terminal(),
            EndScope(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var repeating = Scope.Repeating.Parse(ref parser);

        Assert.NotNull(repeating?.Condition);
    }

    [Fact(DisplayName = "compiled")]
    public void Compiled()
    {
        // compiled while x is nothing { y += 4; }

        List<Token> tokens = new()
        {
            Keyword.Compiled(),
            Keyword.If(),
            Word("x"),
            Word("is"),
            Word("nothing"),
            StartScope(),
            Word("y"),
            AddAssign(),
            Number(4),
            Terminal(),
            EndScope(),
            new Sentinel(),
        };

        Parser parser = new(tokens.AsLinkedList());
        var conditional = Scope.Conditional.Parse(ref parser);

        Assert.NotNull(conditional?.Modifiers);
        Assert.True(conditional.Modifiers.Is<Compiled>());        
    }
}
