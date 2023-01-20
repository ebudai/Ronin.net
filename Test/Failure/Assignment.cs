using Ronin.Compiler;
using Ronin.Grammar.Errors;

namespace Failure;

[Trait("Parser", null)]
public class Assignment
{
    [Fact(DisplayName = "no value")]
    public void NoValue()
    {
        const string declaration = "x =;";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var statements = parser.Parse();

        Assert.Empty(statements);
        Assert.NotNull(parser.Errors);
        Assert.IsType<UnexpectedSyntaxError>(parser.Errors[0]);
    }
}
