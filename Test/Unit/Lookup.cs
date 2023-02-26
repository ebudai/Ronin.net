using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class lookup
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        Token[] tokens = 
        {
            new OpenBrace(),
            new Text(),
            new Assign(),
            new Number(),
            new CloseBrace(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var lookup = InlineLookupSyntax.Parse(ref parser);

        Assert.Single(lookup?.Values);
        var association = lookup.Values[0];
        
        LiteralSyntax key = association.Key;
        Assert.Single(key?.Source);

        LiteralSyntax value = association.Value;
        Assert.Single(value?.Source);
    }

    [Fact(DisplayName = "as value")]
    public void AsValue()
    {
        Token[] tokens =
        {
            new Variable(),
            new Word(),
            new Assign(),
            new OpenBrace(),
            new Text(),
            new Assign(),
            new Number(),
            new CloseBrace(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var statements = parser.Parse();

        Assert.Single(statements);
        DatumDeclarationSyntax datum = statements[0];
        InlineLookupSyntax lookup = datum?.Initializer;
        Assert.NotNull(lookup);
    }
}
