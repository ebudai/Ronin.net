using Ronin.Compiler;
using Ronin.Lexicon;
using Test;
using Type = Ronin.Grammar.Type;

namespace Failure;

[Trait(nameof(Parser), null)]
public class Types : ParsingTests
{
    [Fact(DisplayName = "no identifier")]
    public void NoIdentifier()
    {
        // datatype { };

        List<Token> tokens = new()
        {
            Keyword.Type(),
            StartScope(),
            EndScope(),
            Terminal(),
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var datatype = Type.Parse(ref parser);
        Assert.Null(datatype);
    }
}
