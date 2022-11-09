using Ronin.Compiler;
using Ronin.Grammar;

namespace Unit;

public class Function
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string declaration = "function test(x => integer) { return 7; }";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var result = parser.Parse();

        Assert.NotEmpty(result);
        Assert.IsType<Statement>(result[0]);
        var statement = result[0] as Statement;
        Assert.NotNull(statement.Function);
        Assert.NotEmpty(statement.Function.Identifier.Values);
        Assert.NotNull(statement.Function.Identifier.Values[0].Name);
        Assert.Equal("test", string.Join(' ', statement.Function.Identifier.Values[0].Name.Words));

    }
}
