using Ronin.Compiler;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class lookup
{
    [Fact(DisplayName = "missing assign")]
    public void MissingAssign()
    {
        // { "thing" 4 }

        Token[] tokens =
        {
            new OpenBrace(),
            new Text(),
            new Number(),
            new CloseBrace()
        };
        
        Parser parser = new(tokens);
        var lookup = Lookup.Parse(ref parser);

        Assert.IsNotType<Lookup>(lookup);
    }

    [Fact(DisplayName = "missing key")]
    public void MissingKey()
    {
        // { = 4 }

        Token[] tokens =
        {
            new OpenBrace(),
            new Assign(),
            new Number(),
            new CloseBrace()
        };
        
        Parser parser = new(tokens);
        var lookup = Lookup.Parse(ref parser);

        Assert.IsNotType<Lookup>(lookup);
    }

    [Fact(DisplayName = "missing value")]
    public void MissingValue()
    {
        // { 3 = }
        Token[] tokens =
        {
            new OpenBrace(),
            new Number(),
            new Assign(),
            new CloseBrace()
        };
        
        Parser parser = new(tokens);
        var lookup = Lookup.Parse(ref parser);

        Assert.IsNotType<Lookup>(lookup);
    }
}
