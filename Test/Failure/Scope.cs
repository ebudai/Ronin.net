using Ronin.Compiler;
using Ronin.Grammar.Errors;

namespace Failure;

[Trait("Parser", null)]
public class Scope
{
    [Fact(DisplayName = "missing name")]
    public void MissingName()
    {
        const string sourcecode = "{tes\"t,;,thing};";

        Lexer lexer = new(sourcecode);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var statements = parser.Parse();
        Assert.NotNull(statements);
        Assert.IsType<UnexpectedSyntaxError>(statements[0]);
    }
}
