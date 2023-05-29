using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Parser", null)]
public class Assignment
{
    [Fact(DisplayName = "no value")]
    public void NoValue()
    {
        // thing = ;

        Token[] tokens =
        {
            new Word { sourcecode = "thing".AsMemory() },
            new Assign { sourcecode = new[] { Assign.symbol } },
            new Terminal { sourcecode = new[] { Terminal.symbol } },
        };
        
        Parser parser = new(tokens);
        var assignment = Ronin.Grammar.Assignment.Parse(ref parser);
        
        Assert.Null(assignment);
    }

    [Fact(DisplayName = "not an assignment")]
    public void NotAnAssignment()
    {
        // what (thing) doing ?;

        Token[] tokens =
        {
            new Word { sourcecode = "what".AsMemory() },
            new StartValues { sourcecode = new[] { StartValues.symbol } },
            new Word { sourcecode = "thing".AsMemory() },
            new EndValues { sourcecode = new[] { EndValues.symbol } },
            new Word { sourcecode = "doing".AsMemory() },
            new Ronin.Lexicon.Symbol { sourcecode = "?".AsMemory() },
            new Terminal { sourcecode = new[] { Terminal.symbol } },
        };

        Parser parser = new(tokens);
        var assignment = Ronin.Grammar.Assignment.Parse(ref parser);

        Assert.Null(assignment);
    }

    [Fact(DisplayName = "empty")]
    public void Blank()
    {
        Token[] tokens = { Sentinel.Instance };

        Parser parser = new(tokens);
        var assignment = Ronin.Grammar.Assignment.Parse(ref parser);

        Assert.Null(assignment);
    }
}
