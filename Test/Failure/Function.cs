using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Symbols;
using Test;

namespace Failure;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class function
{
    [Fact(DisplayName = "no identifier")]
    public void NoIdentifier()
    {
        Tokens tokens = new();
        tokens.Add<Function>()
            .Add<OpenBrace>()
            .Add<CloseBrace>();

        Parser parser = new(tokens.ToArray());
        var function = Ronin.Grammar.Function.Parse(ref parser);
        Assert.Null(function);
    }
}
