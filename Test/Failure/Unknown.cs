using Ronin.Compiler;
using Ronin.Lexicon;
using Test;

namespace Failure;

[Trait("Parser", null)]
public class Unknown: ParsingTests
{
    [Fact(DisplayName = "unknown")]
    public void UnknownSyntaxTest()
    {
        // datatype => ;

        List<Token> tokens = new()
        {
            Datatype(),
            Returns(),
            Terminal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var statements = parser.Parse().Values;
        
        Assert.Single(statements);
        Assert.IsType<Ronin.Grammar.Unknown>(statements[0]);
    }
}
