using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Parser", null)]
public class FunctionDeclarations
{
    [Fact(DisplayName = "no identifier")]
    public void NoIdentifier()
    {
        // function { }

        Token[] tokens = 
        {
            new Function { sourcecode = Function.keyword.AsMemory() },
            new StartScope { sourcecode = new[] { StartScope.symbol } },
            new EndScope { sourcecode = new[] { EndScope.symbol } },
        };

        Parser parser = new(tokens);
        var function = FunctionDeclaration.Parse(ref parser);
        
        Assert.Null(function);
    }
}
