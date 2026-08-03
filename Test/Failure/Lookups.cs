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
        // [ "thing" 4 ]

        List<Token> tokens = new()
        {
            StartBracket(),
            Text("thing"),
            Number(4),
            EndBracket(),
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var lookup = Collection.Parse(ref parser) as Collection;

        Assert.IsNotType<Collection>(lookup);
    }

    [Fact(DisplayName = "missing key")]
    public void MissingKey()
    {
        // [ = 4 ]

        List<Token> tokens = new()
        {
            StartBracket(),
            Assign(),
            Number(4),
            EndBracket(),
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var lookup = Collection.Parse(ref parser) as Collection;

        Assert.IsNotType<Collection>(lookup);
    }

    [Fact(DisplayName = "missing value")]
    public void MissingValue()
    {
        // [ 3 = ]

        List<Token> tokens = new()
        {
            StartBracket(),
            Number(3),
            Assign(),
            EndBracket(),
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var lookup = Collection.Parse(ref parser) as Collection;

        Assert.IsType<Collection>(lookup);
        Assert.Single(lookup);
        Assert.IsType<Collection.Element.ExpectedValueError>(lookup[0]);
    }
}
