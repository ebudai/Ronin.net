using Ronin;
using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Punctuation;

namespace Failure;

[Trait("Parser", null)]
public class Inputs
{
    [Fact(DisplayName = "does not start with (")]
    public void NotAnArguments()
    {
        // not an object;

        Token[] tokens = 
        {
            new Word { sourcecode = "not".AsMemory() },
            new Word { sourcecode = "an".AsMemory() },
            new Word { sourcecode = "object".AsMemory() },
            new Terminal { sourcecode = Terminal.symbol.AsMemory() }
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Inputs.Parse(ref parser);

        Assert.Null(arguments);
    }

    [Fact(DisplayName = "blank")]
    public void Blank()
    {
        Token[] tokens = { Sentinel.Instance };
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Inputs.Parse(ref parser);

        Assert.Null(arguments);
    }

    [Fact(DisplayName = "bad separator")]
    public void BadSeparator()
    {
        // (test, (thing;stuff))

        Token[] tokens =
        {
            new OpenParenthesis { sourcecode = OpenParenthesis.symbol.AsMemory() },
            new Word { sourcecode = "test".AsMemory() },
            new Separator { sourcecode = Separator.symbol.AsMemory() },
            new OpenParenthesis { sourcecode = OpenParenthesis.symbol.AsMemory() },
            new Word { sourcecode = "thing".AsMemory() },
            new Terminal { sourcecode = Terminal.symbol.AsMemory() },
            new Word { sourcecode = "stuff".AsMemory() },
            new CloseParenthesis { sourcecode = CloseParenthesis.symbol.AsMemory() },
            new CloseParenthesis { sourcecode = CloseParenthesis.symbol.AsMemory() }
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Inputs.Parse(ref parser);
        
        Assert.Null(arguments);
    }

    [Fact(DisplayName = "terminated incorrectly")]
    public void TerminatedWrong()
    {
        // (test;)

        Token[] tokens =
        {
            new OpenParenthesis{ sourcecode = OpenParenthesis.symbol.AsMemory() },
            new Word { sourcecode = "test".AsMemory() },
            new Terminal { sourcecode = Terminal.symbol.AsMemory() },
            new CloseParenthesis{ sourcecode = CloseParenthesis.symbol.AsMemory() }
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Inputs.Parse(ref parser);
        
        Assert.Null(arguments);
    }
}
