using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Parser", null)]
public class Export
{
    [Fact(DisplayName = "missing identifier")]
    public void MissingIdentifier() 
    {
        // part of ;

        Token[] tokens =
        {
            new PartOf { sourcecode = PartOf.keyword.AsMemory() },
            new Terminal { sourcecode = new[] { Terminal.symbol } },
        };

        Parser parser = new(tokens);
        var export = Ronin.Grammar.Export.Parse(ref parser);

        Assert.Null(export);
    }
}
