using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keyword;
using Ronin.Lexicon.Punctuation;

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
            new PartOf(),
            new Terminal()
        };

        Parser parser = new(tokens);
        var hierarchy = ImportExport.Parse(ref parser);

        Assert.Null(hierarchy);
    }
}
