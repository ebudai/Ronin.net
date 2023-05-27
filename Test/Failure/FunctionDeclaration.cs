using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keyword;
using Ronin.Lexicon.Punctuation;

namespace Failure;

[Trait("Parser", null)]
public class FunctionDeclaration
{
    [Fact(DisplayName = "no identifier")]
    public void NoIdentifier()
    {
        // function { }

        Token[] tokens = 
        {
            new Function { sourcecode = Function.keyword.AsMemory() },
            new OpenBrace { sourcecode = OpenBrace.symbol.AsMemory() },
            new CloseBrace { sourcecode = CloseBrace.symbol.AsMemory() },
        };

        Parser parser = new(tokens);
        var function = Ronin.Grammar.FunctionDeclaration.Parse(ref parser);
        
        Assert.Null(function);
    }
}
