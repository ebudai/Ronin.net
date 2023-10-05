using Ronin.Compiler;
using Ronin.Lexicon;
using Test;

namespace Failure;

[Trait("Parser", null)]
public class Datatypes : ParsingTests
{
    [Fact(DisplayName = "no identifier")]
    public void NoIdentifier()
    {
        // datatype { };

        List<Token> tokens = new()
        {
            Keyword.Datatype(),
            StartScope(),
            EndScope(),
            Terminal(),
        };
        
        Parser parser = new(tokens);
        var datatype = Ronin.Grammar.Type.Declaration.Parse(ref parser);
        Assert.Null(datatype);
    }
}
