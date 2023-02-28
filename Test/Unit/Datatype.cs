using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

namespace Unit;

[Trait("Parser", null)]
public class Datatype
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // datatype Test { }

        Token[] tokens = 
        {
            new DatatypeKeyword(),
            new Word(),
            new OpenBraceSymbol(),
            new CloseBraceSymbol(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var datatype = DatatypeDeclarationSyntax.Parse(ref parser);

        Assert.Single(datatype?.Identifier?.Components);
        Ronin.Grammar.Name name = datatype.Identifier.Components[0];
        Assert.Equal(1, name?.Source.Length);
    }

    [Fact(DisplayName = "with algebra and members")]
    public void Algebra()
    {
        // datatype Algebra Example = number or { var cash => money; var debt => money; }

        Token[] tokens =
        {
            new DatatypeKeyword(),
            new Word(),
            new Word(),
            new AssignSymbol(),
            new Word(),
            new Word(),
            new OpenBraceSymbol(),
            new VariableKeyword(),
            new Word(),
            new ReturnsSymbol(),
            new Word(),
            new TerminalSymbol(),
            new VariableKeyword(),
            new Word(),
            new ReturnsSymbol(),
            new Word(),
            new TerminalSymbol(),
            new CloseBraceSymbol(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var datatype = DatatypeDeclarationSyntax.Parse(ref parser);

        Assert.Single(datatype?.Identifier?.Components);
        Ronin.Grammar.Name algebra = datatype.Algebra.Components[0];
        Assert.Equal(2, algebra?.Source.Length);
        
        Assert.Equal(2, datatype.Body?.Values.Count);

        {
            DatumDeclarationSyntax cash = datatype.Body.Values[0];
            Assert.IsType<VariableKeyword>(cash?.Mutability);
            Assert.Equal(1, cash.Name?.Source.Length);
            Assert.Single(cash.Datatype?.Components);
            Ronin.Grammar.Name type = cash.Datatype.Components[0];
            Assert.Equal(1, type?.Source.Length);
        }

        {
            DatumDeclarationSyntax debt = datatype.Body.Values[1];
            Assert.IsType<VariableKeyword>(debt?.Mutability);
            Assert.Equal(1, debt.Name?.Source.Length);
            Assert.Single(debt.Datatype?.Components);
            Ronin.Grammar.Name type = debt.Datatype.Components[0];
            Assert.Equal(1, type?.Source.Length);
        }        
    }
}
