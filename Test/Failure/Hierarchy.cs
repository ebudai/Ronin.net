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

        Assert.NotEmpty(tokens);

        Parser parser = new(tokens);
        var result = parser.Parse();

        Assert.NotEmpty(result);
        Reference reference = result[0] as Statement;
        Assert.NotNull(reference);
    }

    [Fact(DisplayName = "improperly terminated")]
    public void Unterminated()
    {
        const string unterminated = "part of thing/stuff (";

        Lexer lexer = new(unterminated);
        var tokens = lexer.Lex();

        Assert.NotEmpty(tokens);

        Parser parser = new(tokens);
        var result = parser.Parse();

        Assert.Empty(result);
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
        var result = parser.Parse();

        Assert.Empty(result);
        Assert.NotEmpty(parser.Errors);
        Assert.IsType<UnexpectedSyntaxError>(parser.Errors[0]);
    }
}
