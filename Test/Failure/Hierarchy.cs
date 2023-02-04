using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon.Keywords;
using Test;

namespace Failure;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class hierarchy
{
    [Fact(DisplayName = "missing identifier")]
    public void MissingIdentifier() 
    {
        Tokens tokens = new();

        tokens.Add<PartOf>().Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var hierarchy = Hierarchy.Parse(ref parser);

        Assert.Null(hierarchy);
    }
}
