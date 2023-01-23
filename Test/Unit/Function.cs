using Ronin.Compiler;

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
        Ronin.Grammar.Function function = result[0];
        Assert.NotNull(function);
        Assert.NotEmpty(function.Identifier.Components);
        Ronin.Grammar.Name name = function.Identifier.Components[0];
        Assert.NotNull(name);
        Assert.Equal("test", string.Join(' ', name.Words));

    }
}
