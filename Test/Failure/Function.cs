using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

namespace Failure;

[Trait("Parser", null)]
public class Function
{
    [Fact(DisplayName = "no identifier")]
    public void NoIdentifier()
    {
        // function { }

        Token[] tokens = 
        {
            new FunctionKeyword(),
            new OpenBraceSymbol(),
            new CloseBraceSymbol()
        };

        Parser parser = new(tokens);
        var function = FunctionDeclarationSyntax.Parse(ref parser);
        
        Assert.Null(function);
    }
}
