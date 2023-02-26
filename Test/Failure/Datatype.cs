using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

namespace Failure;

[Trait("Parser", null)]
public class Datatype
{
    [Fact(DisplayName = "no identifier")]
    public void NoIdentifier()
    {
        // datatype { };

        Token[] tokens =
        {
            new DatatypeKeyword(),
            new OpenBraceSymbol(),
            new CloseBraceSymbol(),
            new TerminalSymbol()
        };
        
        Parser parser = new(tokens);
        var datatype = DatatypeDeclarationSyntax.Parse(ref parser);
        Assert.Null(datatype);
    }
}
