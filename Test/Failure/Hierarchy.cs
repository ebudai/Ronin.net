using Ronin.Compiler;
using Ronin.Grammar;

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

        Assert.NotEmpty(result);
    }
}
