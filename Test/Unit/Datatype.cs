using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon.Reserved;

namespace Unit;

[Trait("Parser", null)]
public class Datatype
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string declaration = "datatype Test { }";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var result = parser.Parse();

        Assert.NotEmpty(result);
        Ronin.Grammar.Datatype datatype = result[0] as Statement;
        Assert.NotNull(datatype);
        Assert.NotEmpty(datatype.Identifier.Components);
        var name = datatype.Identifier.Components[0].Syntax as Ronin.Grammar.Name;
        Assert.NotNull(name);
        Assert.Equal("Test", string.Join(' ', name.Words));
    }

    [Fact(DisplayName = "with algebra and members")]
    public void Algebra()
    {
        const string declaration = "datatype Algebra = integer or { var cash => money; var debt => money; }";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var result = parser.Parse();

        Assert.NotEmpty(result);
        Ronin.Grammar.Datatype datatype = result[0] as Statement;
        Assert.NotNull(datatype);
        Assert.Equal(2, datatype.Body.Values.Count);
        
        var cash = datatype.Body.Values[0].Syntax as Ronin.Grammar.Datum;
        Assert.NotNull(cash);
        Assert.IsType<Variable>(cash.Mutability);
        Assert.Equal("cash", cash.Name.Words[0]);
        Assert.NotEmpty(cash.Datatype.Values);
        Ronin.Grammar.Name cashtypename = cash.Datatype.Values[0];
        Assert.Equal("money", string.Join(' ', cashtypename.Words));

        var debt = datatype.Body.Values[1].Syntax as Ronin.Grammar.Datum;
        Assert.NotNull(debt);
        Assert.IsType<Variable>(debt.Mutability);
        Assert.Equal("debt", debt.Name.Words[0]);
        Assert.NotEmpty(debt.Datatype.Values);
        Ronin.Grammar.Name debttypename = debt.Datatype.Values[0];
        Assert.Equal("money", string.Join(' ', debttypename.Words));
    }
}
