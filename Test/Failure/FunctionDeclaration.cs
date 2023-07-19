using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Failure;

[Trait("Parser", null)]
public class FunctionDeclarations : ParsingTests
{
    [Fact(DisplayName = "no identifier")]
    public void NoIdentifier()
    {
        // function { }

        List<Token> tokens = new()
        {
            Function(),
            StartScope(),
            EndScope(),
        };

        Parser parser = new(tokens);
        var function = Ronin.Grammar.Function.Declaration.Parse(ref parser);
        
        Assert.Null(function);
    }
}
