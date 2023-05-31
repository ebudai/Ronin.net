using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Parser", null)]
public class Lookup
{
    [Fact(DisplayName = "missing assign")]
    public void MissingAssign()
    {
        // { "thing" 4 }

        Token[] tokens =
        {
            new StartScope { sourcecode = new[] { StartScope.symbol } },
            new Text { sourcecode = "\"thing\"".AsMemory() },
            new Number { sourcecode = "4".AsMemory() },
            new EndScope { sourcecode = new[] { EndScope.symbol } },
        };
        
        Parser parser = new(tokens);
        var lookup = Ronin.Grammar.Compound.Lookup.Parse(ref parser);

        Assert.IsNotType<Ronin.Grammar.Compound.Lookup>(lookup);
    }

    [Fact(DisplayName = "missing key")]
    public void MissingKey()
    {
        // { = 4 }

        Token[] tokens =
        {
            new StartScope { sourcecode = new[] { StartScope.symbol } },
            new Assign { sourcecode = new[] { Assign.symbol } },
            new Number { sourcecode = "4".AsMemory() },
            new EndScope { sourcecode = new[] { EndScope.symbol } },
        };
        
        Parser parser = new(tokens);
        var lookup = Ronin.Grammar.Compound.Lookup.Parse(ref parser);

        Assert.IsNotType<Ronin.Grammar.Compound.Lookup>(lookup);
    }

    [Fact(DisplayName = "missing value")]
    public void MissingValue()
    {
        // { 3 = }

        Token[] tokens =
        {
            new StartScope { sourcecode = new[] { StartScope.symbol } },
            new Number { sourcecode = "3".AsMemory() },
            new Assign { sourcecode = new[] { Assign.symbol } },
            new EndScope { sourcecode = new[] { EndScope.symbol } },
        };
        
        Parser parser = new(tokens);
        var lookup = Ronin.Grammar.Compound.Lookup.Parse(ref parser);

        Assert.IsNotType<Ronin.Grammar.Compound.Lookup>(lookup);
    }
}
