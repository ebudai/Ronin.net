using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class unknown
{
    [Fact(DisplayName = "unknown")]
    public void Unknown()
    {
        Token[] tokens = 
        {
            new Datatype(),
            new Returns(),
            new Terminal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var statements = parser.Parse();
        
        Assert.Single(statements);
        Ronin.Grammar.Unknown unknown = statements[0];
        Assert.NotNull(unknown);
    }
}
