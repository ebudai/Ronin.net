using Ronin.Compiler;
using Ronin.Grammar;

namespace Failure;

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
        Assert.IsType<Reference>(result[0]);
        var reference = result[0] as Reference;
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
