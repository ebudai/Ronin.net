using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Failure;

[Trait("Parser", null)]
public class ExportKeyword : ParsingTests
{
    [Fact(DisplayName = "missing identifier")]
    public void MissingIdentifier() 
    {
        // part of ;

        List<Token> tokens = new()
        {
            PartOf(),
            Terminal(),
        };

        Parser parser = new(tokens);
        var export = Export.Parse(ref parser);

        Assert.Null(export);
    }
}
