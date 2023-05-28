using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Parser", null)]
public class DatatypeDeclaration
{
    [Fact(DisplayName = "no identifier")]
    public void NoIdentifier()
    {
        // datatype { };

        Token[] tokens =
        {
            new Datatype(),
            new StartScope(),
            new EndScope(),
            new Terminal(),
        };
        
        Parser parser = new(tokens);
        var datatype = Ronin.Grammar.DatatypeDeclaration.Parse(ref parser);
        Assert.Null(datatype);
    }
}
