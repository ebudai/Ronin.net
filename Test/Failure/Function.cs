using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Errors;

namespace Failure;

[Trait("Parser", null)]
public class Function
{
    [Fact(DisplayName = "bad name")]
    public void BadName()
    {
        const string line = "function test) thing(x => number) { }";

        Lexer lexer = new(line);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var statements = parser.Parse();

        Assert.NotEmpty(statements);
        var statement = statements[0] as Statement;
        Assert.Null(statement);
        var error = statements[0] as Error;
        Assert.NotNull(error);
        Assert.IsType<UnexpectedSyntaxError>(error);
    }
}
