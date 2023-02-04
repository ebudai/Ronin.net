using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Symbols;
using Test;

namespace Failure;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class datatype
{
    [Fact(DisplayName = "no identifier")]
    public void NoIdentifier()
    {
        Tokens tokens = new();
        tokens.Add<Datatype>()
            .Add<OpenBrace>()
            .Add<CloseBrace>()
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var datatype = Ronin.Grammar.Datatype.Parse(ref parser);
        Assert.Null(datatype);
    }
}
