using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

namespace Failure;

[Trait("Parser", null)]
public class Unknown
{
    [Fact(DisplayName = "unknown")]
    public void UnknownSyntaxTest()
    {
        Token[] tokens = 
        {
            new DatatypeKeyword(),
            new ReturnsSymbol(),
            new TerminalSymbol(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var statements = parser.Parse().Values;
        
        Assert.Single(statements);
        UnknownSyntax unknown = statements[0];
        Assert.NotNull(unknown);
    }
}
