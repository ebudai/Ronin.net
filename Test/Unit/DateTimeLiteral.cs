using Ronin.Compiler;

namespace Unit;

public class DateTimeLiteral
{
    [Fact(DisplayName = "two digits with spaced suffix")]
    public void TwoDigitWithSpacedSuffix()
    {
        const string literal = "1984-04-22 11:45:12 p";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Ronin.Tokens.Literals.DateTimeLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "two digits with unspaced suffix")]
    public void TwoDigitWithUnspacedSuffix()
    {
        const string literal = "2022-01-44 10:15:02p";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Ronin.Tokens.Literals.DateTimeLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "two digits without suffix")]
    public void TwoDigitWithoutSuffix()
    {
        const string literal = "1477-04-08 12:30:59";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Ronin.Tokens.Literals.DateTimeLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "one digit with spaced suffix")]
    public void OneDigitWithSpacedSuffix()
    {
        const string literal = "0744-44-20 9:08:45 p";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Ronin.Tokens.Literals.DateTimeLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "one digit with unspaced suffix")]
    public void OneDigitWithUnspacedSuffix()
    {
        const string literal = "1212-18-77 2:22:18p";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Ronin.Tokens.Literals.DateTimeLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "two digit with spaced no suffix")]
    public void TwoDigitWithSpacedNoSuffix()
    {
        const string literal = "3517-08-88 17:22:18 ";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Ronin.Tokens.Literals.DateTimeLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.Trim().ToArray(), lexed.Sourcecode.ToArray());
    }
}
