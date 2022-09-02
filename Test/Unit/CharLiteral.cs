using Ronin.Compiler;

namespace Unit;

public class CharLiteral
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string literal = "'c'";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Literals.CharLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "unicode")]
    public void Unicode()
    {
        const string literal = "'\u44A2'";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Literals.CharLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }
}
