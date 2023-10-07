using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Failure;

[Trait("Parser", null)]
public class Scopes : ParsingTests
{
    [Fact(DisplayName = "missing name")]
    public void MissingName()
    {
        // { ",;,thing }

        List<Token> tokens = new()
        {
            StartScope(),
            TextDelimiter(),
            Separator(),
            Terminal(),
            Separator(),
            Word("thing"),
            EndScope(),
        };
        
        Parser parser = new(tokens);
        var scope = Scope.Parse(ref parser);

        Assert.Null(scope);
    }
}
