using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;
using DelegateSyntax = Ronin.Grammar.DelegateSyntax;

namespace Failure;

[Trait("Parser", null)]
#pragma warning disable IDE1006
public class @delegate
{
    [Fact(DisplayName = "missing returns symbol")]
    public void MissingReturns()
    {
        Token[] tokens =
        {
            new OpenParenthesis(),
            new Word(),
            new Separator(),
            new Word(),
            new Separator(),
            new Word(),
            new CloseParenthesis(),
            new OpenBrace(),
            new Word(),
            new Number(),
            new Terminal(),
            new CloseBrace(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var @delegate = DelegateSyntax.Parse(ref parser);
        
        Assert.Null(@delegate);
    }
}
