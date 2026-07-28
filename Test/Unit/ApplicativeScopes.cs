using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait(nameof(Parser), null)]
public class ApplicativeScopes : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // compiled hidden { var deadline = time tomorrow; }

        List<Token> tokens = new()
        {
            Keyword.Compiled(),
            Keyword.Hidden(),
            StartScope(),
            Keyword.Variable(),
            Word("deadline"),
            Assign(),
            Word("time"),
            Word("tomorrow"),
            Terminal(),
            EndScope(),          // the brace the comment shows and the tokens omitted
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var scope = Scope.Applicative.Parse(ref parser);

        Assert.NotNull(scope);
    }
}
