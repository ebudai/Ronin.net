namespace Unit;

public class Compiled
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string modifier = "compiled ";

        Ronin.Compiler.Lexer lexer = new(modifier);
        var lexed = Ronin.Tokens.Modifiers.Compiled.Lex(lexer);

        Assert.NotNull(lexed);
    }
}
