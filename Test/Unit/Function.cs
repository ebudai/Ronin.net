using Ronin.Compiler;
using Ronin.Grammar;

namespace Unit;

[Trait("Parser", null)]
public class Function
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string declaration = "function test(x => integer) { return 7; }";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var result = parser.Parse();

        Assert.NotEmpty(result);
        Ronin.Grammar.Function function = result[0] as Statement;
        Assert.NotNull(function);
        Assert.NotEmpty(function.Identifier.Components);
        var name = function.Identifier.Components[0].Syntax as Ronin.Grammar.Name;
        Assert.NotNull(name);
        Assert.Equal("test", string.Join(' ', name.Words));

    }
}
