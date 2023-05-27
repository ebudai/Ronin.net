using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Punctuation;

namespace Failure;

[Trait("Parser", null)]
public class DelegateDeclaration
{
    [Fact(DisplayName = "missing returns symbol")]
    public void MissingReturns()
    {
        // (things, stuff, others) { return 3; }

        Token[] tokens =
        {
            new OpenParenthesis { sourcecode = OpenParenthesis.symbol.AsMemory() },
            new Word { sourcecode = "things".AsMemory() },
            new Separator { sourcecode = Separator.symbol.AsMemory() },
            new Word { sourcecode = "stuff".AsMemory() },
            new Separator { sourcecode = Separator.symbol.AsMemory() },
            new Word { sourcecode = "others".AsMemory() },
            new CloseParenthesis { sourcecode = CloseParenthesis.symbol.AsMemory() },
            new OpenBrace { sourcecode = OpenBrace.symbol.AsMemory() },
            new Word { sourcecode = "return".AsMemory() },
            new NumberLiteral { sourcecode = "3".AsMemory() },
            new Terminal { sourcecode = Terminal.symbol.AsMemory() },
            new CloseBrace { sourcecode = CloseBrace.symbol.AsMemory() },
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
            new Word { sourcecode = "billy".AsMemory() },
            new Returns { sourcecode = Returns.symbol.AsMemory() },
            new Terminal { sourcecode = Terminal.symbol.AsMemory() }
        };

        Parser parser = new(tokens);
        var @delegate = Ronin.Grammar.Delegate.Parse(ref parser);

        Assert.Null(@delegate);
    }
}
