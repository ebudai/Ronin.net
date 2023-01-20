using Ronin.Compiler;
using Ronin.Grammar.Errors;
using Ronin.Lexicon.Symbols;

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
        Assert.Empty(statements);
        Assert.NotEmpty(parser.Errors);
        Assert.IsType<ExpectedSyntaxError<Terminal, CloseBrace>>(parser.Errors[0]);
    }
}
