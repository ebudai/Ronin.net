using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Punctuation;

namespace Failure;

[Trait("Parser", null)]
public class Unknown
{
    [Fact(DisplayName = "unknown")]
    public void UnknownSyntaxTest()
    {
        Token[] tokens = 
        {
            new Ronin.Lexicon.Keyword.Datatype(),
            new Returns(),
            new Terminal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var statements = parser.Parse().Values;
        
        Assert.Single(statements);
        Assert.IsType<Ronin.Grammar.Unknown>(statements[0]);
    }
}
