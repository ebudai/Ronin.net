using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Compound;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait("Parser", null)]
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
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var lookup = Lookup.Parse(ref parser);

        Assert.Single(lookup?.Values);
        var association = lookup.Values[0];

        var key = association.Key as Inline;
        Assert.Equal(1, key?.Source.Length);

        var value = association.Value as Inline;
        Assert.Equal(1, value?.Source.Length);
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
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var statements = parser.Parse().Values;

        Assert.Single(statements);
        var datum = statements[0] as Datum.Declaration;
        var lookup = datum?.Initializer as Lookup;
        Assert.NotNull(lookup);
    }
}
