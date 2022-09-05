namespace Unit;

public class TextLiteral
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string literal = "\"testtest\"";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Ronin.Tokens.Literals.TextLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with escaped quotes")]
    public void Escaped()
    {
        const string literal = @"""tes\""tte\""st""";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Ronin.Tokens.Literals.TextLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "multiline")]
    public void Multiline()
    {
        const string literal = "\"test\n\nanother test\"";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Ronin.Tokens.Literals.TextLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(3, lexer.Line);
    }
}