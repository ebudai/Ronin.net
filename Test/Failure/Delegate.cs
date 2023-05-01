using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Punctuation;

namespace Failure;

[Trait("Parser", null)]
public class Delegate
{
    [Fact(DisplayName = "missing returns symbol")]
    public void MissingReturns()
    {
        // (things, stuff, others) { return 3; }

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
            new NumberLiteral(),
            new Terminal(),
            new CloseBrace(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var @delegate = Ronin.Grammar.Delegate.Parse(ref parser);
        
        Assert.Null(@delegate);
    }

    [Fact(DisplayName = "no body")]
    public void NoBody()
    {
        // billy => ;
        Token[] tokens =
        {
            new Word(),
            new Returns(),
            new Terminal()
        };

        Parser parser = new(tokens);
        var @delegate = Ronin.Grammar.Delegate.Parse(ref parser);

        Assert.Null(@delegate);
    }
}
