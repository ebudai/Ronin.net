namespace Unit;

public class Datatype
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string modifier = "datatype ";

        Ronin.Compiler.Lexer lexer = new(modifier);
        var lexed = Ronin.Tokens.Modifiers.Datatype.Lex(lexer);

        Assert.NotNull(lexed);
    }
}
