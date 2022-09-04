namespace Unit;

public class Terminal
{
    [Fact(DisplayName = "terminal")]
    public void Basic()
    {
        const string sourcecode = ";";

        Ronin.Compiler.Lexer lexer = new() { Sourcecode = sourcecode.ToArray() };
        var lexed = Ronin.Tokens.Symbols.Terminal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(sourcecode.ToArray(), lexed.Sourcecode.ToArray());
    }
}
