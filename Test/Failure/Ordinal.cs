using Ronin;
using Ronin.Compiler;
using Ronin.Grammar.Compound;
using Ronin.Lexicon;
using Ronin.Lexicon.Punctuation;

namespace Failure;

[Trait("Parser", null)]
public class Ordinal
{
    [Fact(DisplayName = "does not start with [")]
    public void NotAnOrdinal()
    {
        // not an ordinal;

        Token[] tokens =
        {
            new Word { sourcecode = "not".AsMemory() },
            new Word { sourcecode = "an".AsMemory() },
            new Word { sourcecode = "ordinal".AsMemory() },
            new Terminal { sourcecode = Terminal.symbol.AsMemory() }
        };
        
        Parser parser = new(tokens);
        var ordinal = Ronin.Grammar.Compound.Ordinal.Parse(ref parser);

        Assert.Null(ordinal);
    }

    [Fact(DisplayName = "blank")]
    public void Blank()
    {
        Token[] tokens = { Sentinel.Instance };
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Ordinal.Parse(ref parser);

        Assert.Null(arguments);
    }

    [Fact(DisplayName = "bad component")]
    public void BadComponent()
    {
        // [test, (thing;stuff)]

        Token[] tokens =
        {
            new OpenSquareBracket { sourcecode = OpenSquareBracket.symbol.AsMemory() },
            new Word { sourcecode = "test".AsMemory() },
            new Separator { sourcecode = Separator.symbol.AsMemory() },
            new Word { sourcecode = "thing".AsMemory() },
            new Terminal { sourcecode = Terminal.symbol.AsMemory() },
            new Word { sourcecode = "stuff".AsMemory() },
            new CloseParenthesis { sourcecode = CloseParenthesis.symbol.AsMemory() },
            new CloseSquareBracket { sourcecode = CloseSquareBracket.symbol.AsMemory() }
        };
        
        Parser parser = new(tokens);
        var ordinal = Ronin.Grammar.Compound.Ordinal.Parse(ref parser);

        Assert.Null(ordinal);
    }

    [Fact(DisplayName = "terminated incorrectly")]
    public void TerminatedWrong()
    {
        // [test;]

        Token[] tokens =
        {
            new OpenSquareBracket { sourcecode = OpenSquareBracket.symbol.AsMemory() },
            new Word { sourcecode = "test".AsMemory() },
            new Terminal { sourcecode = Terminal.symbol.AsMemory() },
            new CloseSquareBracket { sourcecode = CloseSquareBracket.symbol.AsMemory() }
        };
        
        Parser parser = new(tokens);
        var ordinal = Ronin.Grammar.Compound.Ordinal.Parse(ref parser);

        Assert.Null(ordinal);
    }
}
