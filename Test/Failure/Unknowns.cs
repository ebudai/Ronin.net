using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Failure;

[Trait("Parser", null)]
public class Unknowns: ParsingTests
{
    [Fact(DisplayName = "unknown")]
    public void UnknownSyntaxTest()
    {
        // datatype => ;

        List<Token> tokens = new()
        {
            Keyword.Datatype(),
            Returns(),
            Terminal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var statements = parser.Parse().Values;
        
        Assert.Single(statements);
        Assert.IsType<Unknown>(statements[0]);
    }
}
