namespace Unit;

public class OpenBrace
{
    [Fact(DisplayName = "open brace")]
    public void Basic()
    {
        const string sourcecode = "{";

        Ronin.Compiler.Lexer lexer = new() { Sourcecode = sourcecode.ToArray() };
        var lexed = Ronin.Tokens.Symbols.OpenBrace.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(sourcecode.ToArray(), lexed.Sourcecode.ToArray());
    }
}
