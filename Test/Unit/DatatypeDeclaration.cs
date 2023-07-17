using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class DatatypeDeclaration : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // datatype Test { }

        List<Token> tokens = new()
        {
            Datatype(),
            Word("Test"),
            StartScope(),
            EndScope(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var datatype = Ronin.Grammar.DatatypeDeclaration.Parse(ref parser);

        Assert.Single(datatype?.Name?.Components);
        Ronin.Grammar.Words name = datatype.Name.Components[0];
        Assert.Equal(1, name?.Source.Length);
    }

    [Fact(DisplayName = "with algebra and members")]
    public void Algebra()
    {
        // datatype Algebra Example = number or { var cash => money; var debt => money; }

        List<Token> tokens = new()
        {
            Datatype(),
            Word("Algebra"),
            Word("Example"),
            Assign(),
            Word("number"),
            Word("or"),
            StartScope(),
            Variable(),
            Word("cash"),
            Returns(),
            Word("money"),
            Terminal(),
            Variable(),
            Word("debt"),
            Returns(),
            Word("money"),
            Terminal(),
            EndScope(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var datatype = Ronin.Grammar.DatatypeDeclaration.Parse(ref parser);

        Assert.Single(datatype?.Name?.Components);
        Ronin.Grammar.Words algebra = datatype.Algebra.Components[0];
        Assert.Equal(2, algebra?.Source.Length);
        
        Assert.Equal(2, datatype.Definition?.Values.Count);

        {
            var cash = datatype.Definition.Values[0] as Ronin.Grammar.DatumDeclaration;
            Assert.IsType<Variable>(cash?.Mutability);
            Assert.Equal(1, cash.Name?.Source.Length);
            Assert.Single(cash.Datatype?.Components);
            Ronin.Grammar.Words type = cash.Datatype.Components[0];
            Assert.Equal(1, type?.Source.Length);
        }

        {
            var debt = datatype.Definition.Values[1] as Ronin.Grammar.DatumDeclaration;
            Assert.IsType<Variable>(debt?.Mutability);
            Assert.Equal(1, debt.Name?.Source.Length);
            Assert.Single(debt.Datatype?.Components);
            Ronin.Grammar.Words type = debt.Datatype.Components[0];
            Assert.Equal(1, type?.Source.Length);
        }        
    }
}
