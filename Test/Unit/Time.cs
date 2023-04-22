using Ronin;
using Ronin.Compiler;
using Ronin.Lexicon;

namespace Unit;

[Trait("Lexer", null)]
public class Time
{
    [Fact(DisplayName = "two digits with spaced suffix")]
    public void TwoDigitWithSpacedSuffix()
    {
        const string literal = "11:45:12 p";

        Lexer lexer = new(literal);
        var time = Literal.Lex(ref lexer) as TimeLiteral;

        Assert.Equal(literal, time?.ToString());
    }

    [Fact(DisplayName = "two digits with unspaced suffix")]
    public void TwoDigitWithUnspacedSuffix()
    {
        const string literal = "10:15:02p";

        Lexer lexer = new(literal);
        var time = Literal.Lex(ref lexer) as TimeLiteral;

        Assert.Equal(literal, time?.ToString());
    }

    [Fact(DisplayName = "two digits without suffix")]
    public void TwoDigitWithoutSuffix()
    {
        const string literal = "12:30:59";

        Lexer lexer = new(literal);
        var time = Literal.Lex(ref lexer) as TimeLiteral;

        Assert.Equal(literal, time?.ToString());
    }

    [Fact(DisplayName = "one digit with spaced suffix")]
    public void OneDigitWithSpacedSuffix()
    {
        const string literal = "9:08:45 p";

        Lexer lexer = new(literal);
        var time = Literal.Lex(ref lexer) as TimeLiteral;

        Assert.Equal(literal, time?.ToString());
    }

    [Fact(DisplayName = "one digit with unspaced suffix")]
    public void OneDigitWithUnspacedSuffix()
    {
        const string literal = "2:22:18p";

        Lexer lexer = new(literal);
        var time = Literal.Lex(ref lexer) as TimeLiteral;

        Assert.Equal(literal, time?.ToString());
    }

    [Fact(DisplayName = "two digit with spaced no suffix")]
    public void TwoDigitWithSpacedNoSuffix()
    {
        const string literal = "17:22:18 ";

        Lexer lexer = new(literal);
        var time = Literal.Lex(ref lexer) as TimeLiteral;

        Assert.Equal(literal.Trim(), time?.ToString());
    }
}
