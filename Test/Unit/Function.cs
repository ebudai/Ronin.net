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
        Assert.IsType<Ronin.Grammar.Function>(result[0]);
        var function = result[0] as Ronin.Grammar.Function;
        Assert.NotNull(function);
        Assert.NotEmpty(function.Identifier.Components);
        Ronin.Grammar.Name name = function.Identifier.Components[0];
        Assert.NotNull(name);
        Assert.Equal("test", string.Join(' ', name.Words));

    }
}
