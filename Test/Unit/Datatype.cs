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
        Assert.IsType<Statement>(result[0]);
        var statement = result[0] as Statement;
        Assert.NotNull(statement.DatatypeDeclaration);
        Assert.NotEmpty(statement.DatatypeDeclaration.Identifier.Values);
        Assert.NotNull(statement.DatatypeDeclaration.Identifier.Values[0].Name);
        Assert.Equal("Test", string.Join(' ', statement.DatatypeDeclaration.Identifier.Values[0].Name.Words));
    }
}
