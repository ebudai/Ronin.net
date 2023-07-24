using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Datatypes : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // datatype Test { }

        List<Token> tokens = new()
        {
            Keyword.Datatype(),
            Word("Test"),
            StartScope(),
            EndScope(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var datatype = Ronin.Grammar.Datatype.Declaration.Parse(ref parser);

        Assert.Single(datatype?.Identifier?.Components);
        Identifier.Component name = datatype.Identifier.Components[0];
        Assert.Equal(1, name?.Source.Length);
    }

    [Fact(DisplayName = "with algebra and members")]
    public void Algebra()
    {
        // datatype Algebra Example = number or { var cash => money; var debt => money; }

        List<Token> tokens = new()
        {
            Keyword.Datatype(),
            Word("Algebra"),
            Word("Example"),
            Assign(),
            Word("number"),
            Word("or"),
            StartScope(),
            Keyword.Variable(),
            Word("cash"),
            Returns(),
            Word("money"),
            Terminal(),
            Keyword.Variable(),
            Word("debt"),
            Returns(),
            Word("money"),
            Terminal(),
            EndScope(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var datatype = Ronin.Grammar.Datatype.Declaration.Parse(ref parser);

        Assert.Single(datatype?.Identifier?.Components);
        Name algebra = datatype.Algebra.Components[0];
        Assert.Equal(2, algebra?.Source.Length);
        
        Assert.Equal(2, datatype.Definition?.Values.Count);

        {
            var cash = datatype.Definition.Values[0] as Datum.Declaration;
            Assert.IsType<Variable>(cash?.Mutability);
            Assert.Equal(1, cash.Name?.Source.Length);
            Assert.Single(cash.Datatype?.Components);
            Name type = cash.Datatype.Components[0];
            Assert.Equal(1, type?.Source.Length);
        }

        {
            var debt = datatype.Definition.Values[1] as Datum.Declaration;
            Assert.IsType<Variable>(debt?.Mutability);
            Assert.Equal(1, debt.Name?.Source.Length);
            Assert.Single(debt.Datatype?.Components);
            Name type = debt.Datatype.Components[0];
            Assert.Equal(1, type?.Source.Length);
        }        
    }
}
