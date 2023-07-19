using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Failure;

[Trait("Parser", null)]
public class DatatypeDeclarations : ParsingTests
{
    [Fact(DisplayName = "no identifier")]
    public void NoIdentifier()
    {
        // datatype { };

        List<Token> tokens = new()
        {
            Datatype(),
            StartScope(),
            EndScope(),
            Terminal(),
        };
        
        Parser parser = new(tokens);
        var datatype = Ronin.Grammar.Datatype.Declaration.Parse(ref parser);
        Assert.Null(datatype);
    }
}
