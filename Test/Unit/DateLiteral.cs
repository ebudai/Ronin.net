namespace Unit;

public class DateLiteral
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string literal = "1984-05-04";

        Ronin.Compiler.Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Literals.DateLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }
}
