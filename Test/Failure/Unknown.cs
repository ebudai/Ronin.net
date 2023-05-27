using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keyword;
using Ronin.Lexicon.Punctuation;

namespace Failure;

[Trait("Parser", null)]
public class Unknown
{
    [Fact(DisplayName = "unknown")]
    public void UnknownSyntaxTest()
    {
        // datatype => ;

        Token[] tokens = 
        {
            new Datatype { sourcecode = Datatype.keyword.AsMemory() },
            new Returns { sourcecode = Returns.symbol.AsMemory() },
            new Terminal { sourcecode = Terminal.symbol.AsMemory() },
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var statements = parser.Parse().Values;
        
        Assert.Single(statements);
        Assert.IsType<Ronin.Grammar.Unknown>(statements[0]);
    }
}
