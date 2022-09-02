using Ronin.Compiler;

namespace Unit;

public class TimeLiteral
{
    [Fact(DisplayName = "two digits with spaced suffix")]
    public void TwoDigitWithSpacedSuffix()
    {
        const string literal = "11:45:12 p";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Literals.TimeLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "two digits with unspaced suffix")]
    public void TwoDigitWithUnspacedSuffix()
    {
        const string literal = "10:15:02p";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Literals.TimeLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "two digits without suffix")]
    public void TwoDigitWithoutSuffix()
    {
        const string literal = "12:30:59";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Literals.TimeLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "one digit with spaced suffix")]
    public void OneDigitWithSpacedSuffix()
    {
        const string literal = "9:08:45 p";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Literals.TimeLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "one digit with unspaced suffix")]
    public void OneDigitWithUnspacedSuffix()
    {
        const string literal = "2:22:18p";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Literals.TimeLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "two digit with spaced no suffix")]
    public void TwoDigitWithSpacedNoSuffix()
    {
        const string literal = "17:22:18 ";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Ronin.Tokens.Literals.TimeLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.Trim().ToArray(), lexed.Sourcecode.ToArray());
    }
}
