using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Failure;

[Trait("Parser", null)]
public class RepeatingScopes : ParsingTests
{
    [Fact(DisplayName = "no condition")]
    public void NoCondition()
    {
        // while { y -= 2; }

        List<Token> tokens = new()
        {
            Keyword.While(),
            StartScope(),
            Word("y"),
            SubtractAssign(),
            Number(2),
            Terminal(),
            EndScope(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var repeating = RepeatingScope.Parse(ref parser);

        Assert.Null(repeating);
    }

    [Fact(DisplayName = "no definition")]
    public void NoDefinition()
    {
        // while x is nothing;

        List<Token> tokens = new()
        {
            Keyword.Compiled(),
            Keyword.While(),
            Word("x"),
            Word("is"),
            Word("nothing"),
            Terminal(),
        };

        Parser parser = new(tokens);
        var repeating = RepeatingScope.Parse(ref parser);

        Assert.Null(repeating);
    }
}
