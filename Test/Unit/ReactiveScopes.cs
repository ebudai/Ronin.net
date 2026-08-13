using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait(nameof(Parser), null)]
public class ReactiveScopes : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // when changing x => y = x;

        List<Token> tokens = new()
        {
            Keyword.When(),
            Keyword.Changing(),
            Word("x"),
            Arrow(),
            Word("y"),
            Assign(),
            Word("x"),
            Terminal(),
            EndScope(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var reactive = Scope.Reactive.Parse(ref parser);

        Assert.NotNull(reactive);
    }
}
