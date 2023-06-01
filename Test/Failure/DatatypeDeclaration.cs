using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Parser", null)]
public class DatatypeDeclarations
{
    [Fact(DisplayName = "no identifier")]
    public void NoIdentifier()
    {
        // datatype { };

        Token[] tokens =
        {
            new Datatype { sourcecode = Datatype.keyword.AsMemory() },
            new StartScope { sourcecode = new[] { StartScope.symbol } },
            new EndScope { sourcecode = new[] { EndScope.symbol } },
            new Terminal { sourcecode = new[] { Terminal.symbol } },
        };
        
        Parser parser = new(tokens);
        var datatype = DatatypeDeclaration.Parse(ref parser);
        Assert.Null(datatype);
    }
}
