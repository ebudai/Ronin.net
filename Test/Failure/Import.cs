using Ronin.Compiler;
using Ronin.Lexicon;
using Test;

namespace Failure;

[Trait("Parser", null)]
public class ImportKeyword : ParsingTests
{
    [Fact(DisplayName = "missing identifier")]
    public void MissingIdentifier() 
    {
        // import ;

        List<Token> tokens = new()
        {
            Import(),
            Terminal(),
        };

        Parser parser = new(tokens);
        var import = Ronin.Grammar.Import.Parse(ref parser);

        Assert.Null(import);
    }
}
