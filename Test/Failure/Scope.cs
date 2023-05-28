using Ronin;
using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Parser", null)]
public class Scope
{
    [Fact(DisplayName = "missing name")]
    public void MissingName()
    {
        // { ",;,thing }

        Token[] tokens =
        {
            new StartScope { sourcecode = new[] { StartScope.symbol } },
            new TextDelimiter { sourcecode = new[] { TextDelimiter.symbol } },
            new Separator { sourcecode = new[] { Separator.symbol } },
            new Terminal { sourcecode = new[] { Terminal.symbol } },
            new Separator { sourcecode = new[] { Separator.symbol } },
            new Word { sourcecode = "thing".AsMemory() },
            new EndScope { sourcecode = new[] { EndScope.symbol } },
        };
        
        Parser parser = new(tokens);
        var scope = Ronin.Grammar.Compound.Scope.Parse(ref parser);

        Assert.Null(scope);
    }
}
