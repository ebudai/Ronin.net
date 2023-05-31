using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;

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
            new StartValues { sourcecode = new[] { StartValues.symbol } },
            new Word { sourcecode = "things".AsMemory() },
            new Separator { sourcecode = new[] { Separator.symbol } },
            new Word { sourcecode = "stuff".AsMemory() },
            new Separator { sourcecode = new[] { Separator.symbol } },
            new Word { sourcecode = "others".AsMemory() },
            new EndValues { sourcecode = new[] { EndValues.symbol } },
            new StartScope { sourcecode = new[] { StartScope.symbol } },
            new Word { sourcecode = "return".AsMemory() },
            new Number { sourcecode = "3".AsMemory() },
            new Terminal { sourcecode = new[] { Terminal.symbol } },
            new EndScope { sourcecode = new[] { EndScope.symbol } },
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
            new Terminal { sourcecode = new[] { Terminal.symbol } },
        };

        Parser parser = new(tokens);
        var @delegate = Ronin.Grammar.Delegate.Parse(ref parser);

        Assert.Null(@delegate);
    }
}
