using Ronin.Compiler;
using Ronin.Grammar;

namespace Unit;

public class Index
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string line = "var test => integer[4];";

        Lexer lexer = new(line);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var statements = parser.Parse();

        Assert.NotEmpty(statements);
        Assert.IsType<Statement>(statements[0]);
        var statement = statements[0] as Statement;
        Assert.NotNull(statement.DatumDeclaration);
        Assert.NotNull(statement.DatumDeclaration.Datatype);
        Assert.NotEmpty(statement.DatumDeclaration.Datatype.Values);
        Assert.NotNull(statement.DatumDeclaration.Datatype.Values[0].Name);
    }
}
