using Ronin.Compiler;
using Ronin.Grammar;

namespace Failure;

public class PartOf
{
    [Fact(DisplayName = "missing name")]
    public void MissingName()
    {
        const string somethingelse = "part of;";

        Lexer lexer = new(somethingelse);
        var tokens = lexer.Lex();

        Assert.NotEmpty(tokens);

        Parser parser = new(tokens);
        var result = parser.Parse();

        Assert.NotEmpty(result);
        Assert.IsAssignableFrom<Expected>(result[0]);
    }
}
