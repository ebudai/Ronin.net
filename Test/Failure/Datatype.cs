using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Errors;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Parser", null)]
public class Datatype
{
    [Fact(DisplayName = "no identifier")]
    public void NoIdentifier()
    {
        const string declaration = "datatype { };";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var statements = parser.Parse();

        Assert.Empty(statements);
        Assert.NotEmpty(parser.Errors);
        var error = parser.Errors[0];
        Assert.IsType<UnexpectedSyntaxError>(error);
    }

    [Fact(DisplayName = "no scope")]
    public void NoScope()
    {
        const string declaration = "datatype x;";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var statements = parser.Parse();

        Assert.Empty(statements);
        Assert.NotEmpty(parser.Errors);
        var error = parser.Errors[0];
        Assert.IsType<ExpectedSyntaxError<OpenBrace, Assign>>(error);
    }
}
