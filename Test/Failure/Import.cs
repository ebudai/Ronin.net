using Ronin.Compiler;
using Ronin.Lexicon;

namespace Failure;

[Trait("Parser", null)]
public class Import
{
    [Fact(DisplayName = "missing identifier")]
    public void MissingIdentifier() 
    {
        // import ;

        Token[] tokens =
        {
            new Ronin.Lexicon.Keyword.Import(),
            new Terminal()
        };

        Parser parser = new(tokens);
        var import = Ronin.Grammar.Import.Parse(ref parser);

        Assert.Null(import);
    }
}
