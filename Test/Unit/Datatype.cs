using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Declaration;

namespace Unit;

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
        Assert.IsType<Ronin.Grammar.Declaration.Datatype>(result[0]);
        var datatype = result[0] as Ronin.Grammar.Declaration.Datatype;
        Assert.NotNull(datatype);
        Assert.NotEmpty(datatype.Identifier.Components);
        Ronin.Grammar.Name name = datatype.Identifier.Components[0];
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
        Assert.IsType<Ronin.Grammar.Declaration.Datatype>(result[0]);
        var datatype = result[0] as Ronin.Grammar.Declaration.Datatype;
        Assert.NotNull(datatype);
        Assert.NotNull(datatype.Algebra);
        Assert.Equal(2, datatype.Body.Values.Length);
        
        Ronin.Grammar.Declaration.Datum cash = datatype.Body.Values[0];
        Assert.NotNull(cash);
        Assert.Equal(Ronin.Grammar.Declaration.Datum.Declarator.Variable, cash.Mutability);
        Assert.Equal("cash", cash.Name.Words[0]);
        Assert.NotEmpty(cash.Datatype.Values);
        Ronin.Grammar.Name cashtypename = cash.Datatype.Values[0];
        Assert.Equal("money", string.Join(' ', cashtypename.Words));

        Ronin.Grammar.Declaration.Datum debt = datatype.Body.Values[1];
        Assert.NotNull(debt);
        Assert.Equal(Ronin.Grammar.Declaration.Datum.Declarator.Variable, debt.Mutability);
        Assert.Equal("debt", debt.Name.Words[0]);
        Assert.NotEmpty(debt.Datatype.Values);
        Ronin.Grammar.Name debttypename = debt.Datatype.Values[0];
        Assert.Equal("money", string.Join(' ', debttypename.Words));
    }
}
