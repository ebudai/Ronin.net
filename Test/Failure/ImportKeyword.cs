using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Parser", null)]
public class ImportKeyword
{
    [Fact(DisplayName = "missing identifier")]
    public void MissingIdentifier() 
    {
        // import ;

        Token[] tokens =
        {
            new Import { sourcecode = Import.keyword.AsMemory() },
            new Terminal { sourcecode = new[] { Terminal.symbol } },
        };

        Parser parser = new(tokens);
        var import = Ronin.Grammar.Import.Parse(ref parser);

        Assert.Null(import);
    }
}
