using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

namespace Failure;

[Trait("Parser", null)]
public class Delegate
{
    [Fact(DisplayName = "missing returns symbol")]
    public void MissingReturns()
    {
        Token[] tokens =
        {
            new OpenParenthesisSymbol(),
            new Word(),
            new SeparatorSymbol(),
            new Word(),
            new SeparatorSymbol(),
            new Word(),
            new CloseParenthesisSymbol(),
            new OpenBraceSymbol(),
            new Word(),
            new NumberLiteral(),
            new TerminalSymbol(),
            new CloseBraceSymbol(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var @delegate = DelegateSyntax.Parse(ref parser);
        
        Assert.Null(@delegate);
    }
}
