using Ronin.Compiler;
using Ronin.Lexicon;
using Test;
using Type = Ronin.Grammar.Type;

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
        var datatype = Type.Parse(ref parser);
        Assert.Null(datatype);
    }
}
