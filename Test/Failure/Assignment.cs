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
}
