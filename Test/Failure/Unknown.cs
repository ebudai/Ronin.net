using Ronin.Compiler;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Symbols;
using Test;

namespace Failure;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class unknown
{
    [Fact(DisplayName = "unknown")]
    public void Unknown()
    {
        Tokens tokens = new();
        tokens.Add<Datatype>()
            .Add<Returns>()
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var statements = parser.Parse();
        Assert.Single(statements);
        Ronin.Grammar.Unknown unknown = statements[0];
        Assert.NotNull(unknown);
    }
}
