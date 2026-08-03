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
        // [ "dave" = 3 ]

        List<Token> tokens = new()
        {
            StartBracket(),
            Text("dave"),
            Assign(),
            Number(3),
            EndBracket(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var lookup = Collection.Parse(ref parser) as Collection;

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
            StartBracket(),
            Text("stuff"),
            Assign(),
            Number(4),
            EndBracket(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var datum = Datum.Parse(ref parser);
        var lookup = datum?.Initializer as Collection;
        Assert.NotNull(lookup);
    }
}
