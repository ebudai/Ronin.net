using Ronin.Compiler;
using Ronin.Grammar.Compound;
using Ronin.Lexicon;
using Ronin.Lexicon.Punctuation;

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
            new OpenBrace { sourcecode = OpenBrace.symbol.AsMemory() },
            new TextLiteral { sourcecode = "\"thing\"".AsMemory() },
            new NumberLiteral { sourcecode = "4".AsMemory() },
            new CloseBrace { sourcecode = CloseBrace.symbol.AsMemory() }
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
            new OpenBrace { sourcecode = OpenBrace.symbol.AsMemory() },
            new Assign { sourcecode = Assign.symbol.AsMemory() },
            new NumberLiteral { sourcecode = "4".AsMemory() },
            new CloseBrace { sourcecode = CloseBrace.symbol.AsMemory() }
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
            new OpenBrace{ sourcecode = OpenBrace.symbol.AsMemory() },
            new NumberLiteral { sourcecode = "3".AsMemory() },
            new Assign { sourcecode = Assign.symbol.AsMemory() },
            new CloseBrace { sourcecode = CloseBrace.symbol.AsMemory() }
        };
        
        Parser parser = new(tokens);
        var lookup = Ronin.Grammar.Compound.Lookup.Parse(ref parser);

        Assert.IsNotType<Ronin.Grammar.Compound.Lookup>(lookup);
    }
}
