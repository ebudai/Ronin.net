using Ronin;
using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Punctuation;

namespace Failure;

[Trait("Parser", null)]
public class Datatype
{
    [Fact(DisplayName = "no identifier")]
    public void NoIdentifier()
    {
        // datatype { };

        Token[] tokens =
        {
            new Ronin.Lexicon.Keyword.Datatype(),
            new OpenBrace(),
            new CloseBrace(),
            new Terminal()
        };
        
        Parser parser = new(tokens);
        var datatype = Ronin.Grammar.Datatype.Parse(ref parser);
        Assert.Null(datatype);
    }
}
