using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Punctuation;

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
            new Ronin.Lexicon.Keyword.Function(),
            new OpenBrace(),
            new CloseBrace()
        };

        Parser parser = new(tokens);
        var function = Ronin.Grammar.Function.Parse(ref parser);
        
        Assert.Null(function);
    }
}
