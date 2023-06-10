using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Lookup : ParsingTests
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
        var lookup = Ronin.Grammar.Compound.Lookup.Parse(ref parser);

        Assert.Single(lookup?.Values);
        var association = lookup.Values[0];

        var key = association.Key as Ronin.Grammar.Literal;
        Assert.Equal(1, key?.Source.Length);

        var value = association.Value as Ronin.Grammar.Literal;
        Assert.Equal(1, value?.Source.Length);
    }

    [Fact(DisplayName = "as value")]
    public void AsValue()
    {
        // var x = { "stuff" = 4 }

        List<Token> tokens = new()
        {
            Variable(),
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
        var datum = statements[0] as Ronin.Grammar.DatumDeclaration;
        var lookup = datum?.Initializer as Ronin.Grammar.Compound.Lookup;
        Assert.NotNull(lookup);
    }
}
