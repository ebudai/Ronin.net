namespace Unit;

public class Separator
{
    [Fact(DisplayName = "separator")]
    public void Basic()
    {
        const string sourcecode = ",";

        Ronin.Compiler.Lexer lexer = new() { Sourcecode = sourcecode.ToArray() };
        var lexed = Ronin.Tokens.Symbols.Separator.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(sourcecode.ToArray(), lexed.Sourcecode.ToArray());
    }
}
