using Ronin.Compiler;
using Ronin.Grammar;

namespace Failure;

public class Import
{
    [Fact(DisplayName = "missing name")]
    public void MissingName()
    {
        const string somethingelse = "import ;";

        Lexer lexer = new(somethingelse);
        var tokens = lexer.Lex();

        Assert.NotEmpty(tokens);

        Parser parser = new(ref tokens[0]);
        var result = parser.Parse();

        Assert.NotEmpty(result);
        Assert.IsType<Statement>(result[0]);
        var statement = result[0] as Statement;
        Assert.NotNull(statement.Reference);
    }

    [Fact(DisplayName = "improperly terminated")]
    public void Unterminated()
    {
        const string unterminated = "import thing/stuff (";

        Lexer lexer = new(unterminated);
        var tokens = lexer.Lex();

        Assert.NotEmpty(tokens);

        Parser parser = new(ref tokens[0]);
        var result = parser.Parse();

        Assert.NotEmpty(result);
    }
}
