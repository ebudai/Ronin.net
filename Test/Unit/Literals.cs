using Ronin.Compiler;
using Ronin.Lexicon;

namespace Unit;

[Trait(nameof(Lexer), null)]
public class Literals
{
    [Fact(DisplayName = "basic date")]
    public void Date()
    {
        const string literal = "1984-05-04";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer) as Date;

        Assert.Equal(literal, lexed?.Memory.ToString());
    }

    [Fact(DisplayName = "a five-digit year")]
    public void WideYear()
    {
        const string literal = "12345-06-07";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer) as Date;

        // the year is the longest digit run, so this is year 12345 and not a
        // four-digit match that falls back to «12345 - 06 - 07»
        Assert.Equal(literal, lexed?.Memory.ToString());
    }

    [Fact(DisplayName = "a year far past four digits")]
    public void VeryWideYear()
    {
        const string literal = "123456-01-01";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer) as Date;

        // the type admits years to 2^57, so almost every legal year is wider than four
        Assert.Equal(literal, lexed?.Memory.ToString());
    }

    [Fact(DisplayName = "an out-of-range date still lexes")]
    public void OutOfRangeStillLexes()
    {
        const string literal = "2026-13-01";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer) as Date;

        // shape decides the token; range is a later finding. «2026-13-01» is a date
        // literal, never a subtraction — a literal must not change kind by its value
        Assert.Equal(literal, lexed?.Memory.ToString());
    }

    [Fact(DisplayName = "basic text")]
    public void Text()
    {
        const string literal = "\"testtest\"";

        Lexer lexer = new(literal);
        var text = Literal.Lex(ref lexer) as Text;

        Assert.Equal(literal, text?.Memory.ToString());
    }

    [Fact(DisplayName = "with escaped quotes")]
    public void Escaped()
    {
        const string literal = @"""tes\""tte\""st""";

        Lexer lexer = new(literal);
        var text = Literal.Lex(ref lexer) as Text;

        Assert.Equal(literal, text?.Memory.ToString());
    }

    [Fact(DisplayName = "multiline")]
    public void Multiline()
    {
        const string literal = "\"test\n\nanother test\"";

        Lexer lexer = new(literal);
        var text = Literal.Lex(ref lexer) as Text;

        Assert.Equal(literal, text?.Memory.ToString());
    }

    [Fact(DisplayName = "value")]
    public void Value()
    {
        const string literal = "\"testtest\"";

        Lexer lexer = new(literal);
        var text = Literal.Lex(ref lexer) as Text;

        Assert.Equal(literal, text?.Memory.ToString());
    }
}
