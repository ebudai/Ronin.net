using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

namespace Failure;

[Trait("Parser", null)]
public class Datum
{
    [Fact(DisplayName = $"{ReactiveKeyword.keyword} before name")]
    public void ReturnsBeforeName()
    {
        // reactive => 44.3;

        Token[] tokens = 
        {
            new ReactiveKeyword(),
            new ReturnsSymbol(),
            new NumberLiteral(),
            new TerminalSymbol()
        };
        
        Parser parser = new(tokens);
        var datum = DatumDeclarationSyntax.Parse(ref parser);
        
        Assert.Null(datum);
    }

    [Fact(DisplayName = "literal instead of identifier")]
    public void LiteralInsteadOfIdentifier()
    {
        // var 555;

        Token[] tokens = 
        {
            new VariableKeyword(),
            new NumberLiteral(),
            new TerminalSymbol()
        };
        
        Parser parser = new(tokens);
        var datum = DatumDeclarationSyntax.Parse(ref parser);
        
        Assert.Null(datum);
    }
}

