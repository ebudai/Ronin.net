using Ronin.Lexicon;

namespace Unit;

[Trait("Lexer", null)]
public class TimeLiteral
{
    [Fact(DisplayName = "two digits with spaced suffix")]
    public void TwoDigitWithSpacedSuffix()
    {
        const string literal = "11:45:12 p";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "two digits with unspaced suffix")]
    public void TwoDigitWithUnspacedSuffix()
    {
        const string literal = "10:15:02p";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "two digits without suffix")]
    public void TwoDigitWithoutSuffix()
    {
        const string literal = "12:30:59";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "one digit with spaced suffix")]
    public void OneDigitWithSpacedSuffix()
    {
        const string literal = "9:08:45 p";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "one digit with unspaced suffix")]
    public void OneDigitWithUnspacedSuffix()
    {
        const string literal = "2:22:18p";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "two digit with spaced no suffix")]
    public void TwoDigitWithSpacedNoSuffix()
    {
        const string literal = "17:22:18 ";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.Trim().ToArray(), lexed.Sourcecode.ToArray());
    }
}
