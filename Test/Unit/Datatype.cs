using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Symbols;

using Datatype = Ronin.Lexicon.Keywords.Datatype;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class datatype
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // datatype Test { }

        Token[] tokens = 
        {
            new Datatype(),
            new Word(),
            new OpenBrace(),
            new CloseBrace(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var datatype = Ronin.Grammar.DatatypeDeclarationSyntax.Parse(ref parser);

        Assert.Single(datatype?.Identifier?.Components);
        Name name = datatype.Identifier.Components[0];
        Assert.Single(name?.Source);
    }

    [Fact(DisplayName = "with algebra and members")]
    public void Algebra()
    {
        // datatype Algebra Example = number or { var cash => money; var debt => money; }

        Token[] tokens =
        {
            new Datatype(),
            new Word(),
            new Word(),
            new Assign(),
            new Word(),
            new Word(),
            new OpenBrace(),
            new Variable(),
            new Word(),
            new Returns(),
            new Word(),
            new Terminal(),
            new Variable(),
            new Word(),
            new Returns(),
            new Word(),
            new Terminal(),
            new CloseBrace(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var datatype = Ronin.Grammar.DatatypeDeclarationSyntax.Parse(ref parser);

        Assert.Single(datatype?.Identifier?.Components);
        Name algebra = datatype.Algebra.Components[0];
        Assert.Equal(2, algebra?.Source.Length);
        
        Assert.Equal(2, datatype.Body?.Values.Count);

        {
            DatumDeclarationSyntax cash = datatype.Body.Values[0];
            Assert.IsType<Variable>(cash?.Mutability);
            Assert.Single(cash.Name?.Source);
            Assert.Single(cash.Datatype?.Components);
            Name type = cash.Datatype.Components[0];
            Assert.Single(type?.Source);
        }

        {
            DatumDeclarationSyntax debt = datatype.Body.Values[1];
            Assert.IsType<Variable>(debt?.Mutability);
            Assert.Single(debt.Name?.Source);
            Assert.Single(debt.Datatype?.Components);
            Name type = debt.Datatype.Components[0];
            Assert.Single(type?.Source);
        }        
    }
}
