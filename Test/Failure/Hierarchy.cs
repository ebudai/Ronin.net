using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;

namespace Failure;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class hierarchy
{
    [Fact(DisplayName = "missing identifier")]
    public void MissingIdentifier() 
    {
        // part of ;

        Token[] tokens =
        {
            new PartOf(),
            new Terminal()
        };

        Parser parser = new(tokens);
        var hierarchy = ImportExportSyntax.Parse(ref parser);

        Assert.Null(hierarchy);
    }
}
