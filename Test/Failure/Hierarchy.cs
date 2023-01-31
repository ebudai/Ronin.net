using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Errors;

namespace Failure;

[Trait("Parser", null)]
public class Hierarchy
{
    [Fact(DisplayName = "missing name")]
    public void MissingName()
    {
        const string somethingelse = "part of ;";

        Lexer lexer = new(somethingelse);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var statements = parser.Parse();

        Assert.Empty(statements);
        Assert.Single(parser.Errors);
        Assert.IsType<UnexpectedSyntaxError>(parser.Errors[0]);
    }

    [Fact(DisplayName = "improperly terminated")]
    public void Unterminated()
    {
        const string unterminated = "part of thing/stuff (";

        Lexer lexer = new(unterminated);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var statements = parser.Parse();

        Assert.Empty(statements);
        Assert.NotEmpty(parser.Errors);
        Assert.IsType<UnexpectedSyntaxError>(parser.Errors[0]);
    }

    [Fact(DisplayName = "bad name")]
    public void BadName()
    {
        const string bad = "part of thing)stuff;";

        Lexer lexer = new(bad);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var statements = parser.Parse();

        Assert.Empty(statements);
        Assert.NotEmpty(parser.Errors);
        Assert.IsType<UnexpectedSyntaxError>(parser.Errors[0]);
    }
}
