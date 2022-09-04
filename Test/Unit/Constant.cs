using Ronin.Compiler;

namespace Unit;

public class Constant
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string modifier = "constant ";

        Ronin.Compiler.Lexer lexer = new(modifier);
        var lexed = Ronin.Tokens.Modifiers.Constant.Lex(lexer);

        Assert.NotNull(lexed);
    }
}
