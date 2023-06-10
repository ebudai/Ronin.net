using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;
using Test;

namespace Failure;

[Trait("Parser", null)]
public class Lookup : ParsingTests
{
    [Fact(DisplayName = "missing assign")]
    public void MissingAssign()
    {
        // { "thing" 4 }

        List<Token> tokens = new()
        {
            StartScope(),
            Text("thing"),
            Number(4),
            EndScope(),
        };
        
        Parser parser = new(tokens);
        var lookup = Ronin.Grammar.Compound.Lookup.Parse(ref parser);

        Assert.IsNotType<Ronin.Grammar.Compound.Lookup>(lookup);
    }

    [Fact(DisplayName = "missing key")]
    public void MissingKey()
    {
        // { = 4 }

        List<Token> tokens = new()
        {
            StartScope(),
            Assign(),
            Number(4),
            EndScope(),
        };
        
        Parser parser = new(tokens);
        var lookup = Ronin.Grammar.Compound.Lookup.Parse(ref parser);

        Assert.IsNotType<Ronin.Grammar.Compound.Lookup>(lookup);
    }

    [Fact(DisplayName = "missing value")]
    public void MissingValue()
    {
        // { 3 = }

        List<Token> tokens = new()
        {
            StartScope(),
            Number(3),
            Assign(),
            EndScope(),
        };
        
        Parser parser = new(tokens);
        var lookup = Ronin.Grammar.Compound.Lookup.Parse(ref parser);

        Assert.IsNotType<Ronin.Grammar.Compound.Lookup>(lookup);
    }
}
