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

        Assert.Empty(statements);
        Assert.NotEmpty(parser.Errors);
        var error = parser.Errors[0];
        Assert.IsType<UnexpectedSyntaxError>(error);
    }

    [Fact(DisplayName = "no identifier")]
    public void NoIdentifier()
    {
        const string declaration = "function {}";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var statements = parser.Parse();

        Assert.Empty(statements);
        Assert.NotEmpty(parser.Errors);
        var error = parser.Errors[0];
        Assert.IsType<UnexpectedSyntaxError>(error);
    }
}
