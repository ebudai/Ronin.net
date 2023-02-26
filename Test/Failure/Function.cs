using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class function
{
    [Fact(DisplayName = "no identifier")]
    public void NoIdentifier()
    {
        // function { }

        Token[] tokens = 
        {
            new Function(),
            new OpenBrace(),
            new CloseBrace()
        };

        Parser parser = new(tokens);
        var function = Ronin.Grammar.FunctionDeclarationSyntax.Parse(ref parser);
        
        Assert.Null(function);
    }
}
