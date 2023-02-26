using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;

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
            new OpenBraceSymbol(),
            new TextLiteral(),
            new AssignSymbol(),
            new NumberLiteral(),
            new CloseBraceSymbol(),
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
        // var x = { "stuff" = 4 }

        Token[] tokens =
        {
            new VariableKeyword(),
            new Word(),
            new AssignSymbol(),
            new OpenBraceSymbol(),
            new TextLiteral(),
            new AssignSymbol(),
            new NumberLiteral(),
            new CloseBraceSymbol(),
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
