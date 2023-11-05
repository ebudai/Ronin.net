using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;
using Literal = Ronin.Grammar.Literal;

namespace Unit;

[Trait(nameof(Parser), null)]
public class Lookups : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // { "dave" = 3 }

        List<Token> tokens = new()
        {
            StartScope(),
            Text("dave"),
            Assign(),
            Number(3),
            EndScope(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var lookup = Lookup.Parse(ref parser);

        Assert.Single(lookup);
        var association = lookup[0];

        var key = association.Destination as Literal;
        Assert.Single(key?.Tokens.ToArray());

        var value = association.Origin as Literal;
        Assert.Single(value?.Tokens.ToArray());
    }

    [Fact(DisplayName = "as value")]
    public void AsValue()
    {
        // var x = { "stuff" = 4 }

        List<Token> tokens = new()
        {
            Keyword.Variable(),
            Word("x"),
            Assign(),
            StartScope(),
            Text("stuff"),
            Assign(),
            Number(4),
            EndScope(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var datum = Datum.Parse(ref parser);
        var lookup = datum?.Initializer as Lookup;
        Assert.NotNull(lookup);
    }
}
