using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

namespace Failure;

[Trait("Parser", null)]
public class Hierarchy
{
    [Fact(DisplayName = "missing identifier")]
    public void MissingIdentifier() 
    {
        // part of ;

        Token[] tokens =
        {
            new PartOfKeyword(),
            new TerminalSymbol()
        };

        Parser parser = new(tokens);
        var hierarchy = ImportExportSyntax.Parse(ref parser);

        Assert.Null(hierarchy);
    }
}
