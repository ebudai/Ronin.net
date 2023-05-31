using Ronin;
using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Parser", null)]
public class Loop
{
    [Fact(DisplayName = $"doesn't start with {ForEach.keyword}")]
    public void NotALoop()
    {
        // not loop;

        Token[] tokens =
        {
            new Word { sourcecode = "not".AsMemory() },
            new Word { sourcecode = "loop".AsMemory() },
            new Terminal { sourcecode = new[] { Terminal.symbol } },
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var loop = Ronin.Grammar.Loop.Parse(ref parser);

        Assert.Null(loop);
    }

    [Fact(DisplayName = "bad name")]
    public void BadName()
    {
        // for each 7 in best horses { run the horse; }

        Token[] tokens =
        {
            new ForEach { sourcecode = ForEach.keyword.AsMemory() },
            new Number { sourcecode = "7".AsMemory() },
            new Word { sourcecode = "in".AsMemory() },
            new Word { sourcecode = "best".AsMemory() },
            new Word { sourcecode = "horses".AsMemory() },
            new StartScope { sourcecode = new[] { StartScope.symbol } },
            new Word { sourcecode = "run".AsMemory() },
            new Word { sourcecode = "the".AsMemory() },
            new Word { sourcecode = "horse".AsMemory() },
            new Terminal { sourcecode = new[] { Terminal.symbol } },
            new EndScope { sourcecode = new[] { EndScope.symbol } },
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var loop = Ronin.Grammar.Loop.Parse(ref parser);

        Assert.Null(loop);
    }

    [Fact(DisplayName = "missing scope")]
    public void MissingScope()
    {
        // for each car in fast cars car colour = 3;

        Token[] tokens =
        {
            new ForEach { sourcecode = ForEach.keyword.AsMemory() },
            new Word { sourcecode = "car".AsMemory() },
            new Word { sourcecode = "in".AsMemory() },
            new Word { sourcecode = "fast".AsMemory() },
            new Word { sourcecode = "cars".AsMemory() },
            new Word { sourcecode = "car".AsMemory() },
            new Word { sourcecode = "colour".AsMemory() },
            new Assign { sourcecode = new[] { Assign.symbol } },
            new Number { sourcecode = "3".AsMemory() },
            new Terminal { sourcecode = new[] { Terminal.symbol } },
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var loop = Ronin.Grammar.Loop.Parse(ref parser);

        Assert.Null(loop);
    }
}
