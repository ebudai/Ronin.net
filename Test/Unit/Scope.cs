using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

namespace Unit;

[Trait("Parser", null)]
public class Scope
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // { var test = 56; }

        Token[] tokens = 
        {
            new OpenBraceSymbol(),
            new VariableKeyword(),
            new Word(),
            new AssignSymbol(),
            new NumberLiteral(),
            new TerminalSymbol(),
            new CloseBraceSymbol(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var scope = Ronin.Grammar.Aggregates.Scope.Parse(ref parser);

        Assert.Single(scope?.Values);

        DatumDeclarationSyntax datum = scope.Values[0];

        Assert.IsType<VariableKeyword>(datum?.Mutability);

        Assert.Single(datum.Name?.Source);

        Assert.Null(datum.Is);

        LiteralSyntax scalar = datum.Initializer;
        Assert.Single(scalar?.Source);
    }
}
