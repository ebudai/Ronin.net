using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Failure;

[Trait(nameof(Parser), null)]
public class ConditionalScopes : ParsingTests
{
    [Fact(DisplayName = "no condition")]
    public void NoCondition()
    {
        // if { return 2; }

        List<Token> tokens = new()
        {
            Keyword.If(),
            StartScope(),
            Word("return"),
            Number(2),
            Terminal(),
            EndScope(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var conditional = Scope.Conditional.Parse(ref parser);

        Assert.IsType<Scope.Conditional.ExpectedConditionError>(conditional);
    }

    [Fact(DisplayName = "no definition")]
    public void NoDefinition()
    {
        // if x is nothing;

        List<Token> tokens = new()
        {
            Keyword.Compiled(),
            Keyword.If(),
            Word("x"),
            Word("is"),
            Word("nothing"),
            Terminal(),
        };

        Parser parser = new(tokens.AsLinkedList());
        var conditional = Scope.Conditional.Parse(ref parser);

        Assert.Null(conditional);
    }
}
