using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait(nameof(Parser), null)]
public class ConditionalReactiveScopes : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // when x < 2 => y = x;

        List<Token> tokens = new()
        {
            Keyword.When(),
            Keyword.Changing(),
            Word("x"),
            Symbol("<"),
            Number(2),
            Returns(),
            Word("y"),
            Assign(),
            Word("x"),
            Terminal(),
            EndScope(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var reactive = Scope.ConditionalReactive.Parse(ref parser);

        Assert.NotNull(reactive);
    }
}
