using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Failure;

[Trait(nameof(Parser), null)]
public class Lookups : ParsingTests
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
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var lookup = Lookup.Parse(ref parser);

        Assert.IsNotType<Lookup>(lookup);
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
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var lookup = Lookup.Parse(ref parser);

        Assert.IsNotType<Lookup>(lookup);
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
        
        Parser parser = new(tokens.AsLinkedList());
        var lookup = Lookup.Parse(ref parser);

        Assert.IsType<Lookup>(lookup);
        Assert.Single(lookup);
        Assert.IsType<Association.ExpectedValueError>(lookup[0]);
    }
}
