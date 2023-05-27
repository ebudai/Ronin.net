using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Compound;
using Ronin.Lexicon;
using Ronin.Lexicon.Keyword;
using Ronin.Lexicon.Punctuation;

namespace Unit;

[Trait("Parser", null)]
public class Lookup
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // { "dave" = 3 }

        Token[] tokens = 
        {
            new OpenBrace(),
            new TextLiteral(),
            new Assign(),
            new NumberLiteral(),
            new CloseBrace(),
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

        Token[] tokens =
        {
            new Variable(),
            new Word(),
            new Assign(),
            new OpenBrace(),
            new TextLiteral(),
            new Assign(),
            new NumberLiteral(),
            new CloseBrace(),
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
