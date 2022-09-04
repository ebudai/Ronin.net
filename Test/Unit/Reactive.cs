namespace Unit;

public class Reactive
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string modifier = "reactive ";

        Ronin.Compiler.Lexer lexer = new() { Sourcecode = modifier.ToArray() };
        var lexed = Ronin.Tokens.Modifiers.Reactive.Lex(lexer);

        Assert.NotNull(lexed);
    }
}
