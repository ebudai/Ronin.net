using Ronin.Compiler;

namespace Unit;

public class Function
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string modifier = "function ";

        Ronin.Compiler.Lexer lexer = new(modifier);
        var lexed = Ronin.Tokens.Modifiers.Function.Lex(lexer);

        Assert.NotNull(lexed);
    }
}
