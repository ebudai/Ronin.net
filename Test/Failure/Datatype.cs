using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class datatype
{
    [Fact(DisplayName = "no identifier")]
    public void NoIdentifier()
    {
        Token[] tokens =
        {
            new Datatype(),
            new OpenBrace(),
            new CloseBrace(),
            new Terminal()
        };
        
        Parser parser = new(tokens);
        var datatype = Ronin.Grammar.Datatype.Parse(ref parser);
        Assert.Null(datatype);
    }
}
