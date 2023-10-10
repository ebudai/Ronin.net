using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Failure;

[Trait(nameof(Parser), null)]
public class ReactiveScopes : ParsingTests
{
    [Fact(DisplayName = "no target")]
    public void NoTarget()
    {
        // when changing => y = x;

        List<Token> tokens = new()
        {
            Keyword.When(),
            Keyword.Changing(),
            Returns(),
            Word("y"),
            Assign(),
            Word("x"),
            Terminal(),
            EndScope(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var reactive = Scope.Reactive.Parse(ref parser);

        Assert.IsType<Scope.Reactive.ExpectedTargetError>(reactive);
    }

    [Fact(DisplayName = "no definition")]
    public void NoDefinition()
    {
        // when changing x;

        List<Token> tokens = new()
        {
            Keyword.When(),
            Keyword.Changing(),
            Word("x"),
            Terminal(),
            EndScope(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var reactive = Scope.Reactive.Parse(ref parser);

        Assert.IsType<Scope.Reactive.ExpectedDefinitionError>(reactive);
    }
}
