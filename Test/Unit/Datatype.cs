using Ronin.Compiler;
using Ronin.Grammar;

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
        Name name = datatype.Identifier.Components[0];
        Assert.NotNull(name);
        Assert.Equal("Test", string.Join(' ', name.Words));
    }

    [Fact(DisplayName = "with algebra")]
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
    }
}
