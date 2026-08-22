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

    [Fact(DisplayName = "the digit alphabet is ASCII 0-9")]
    public void TheDigitAlphabetIsAscii()
    {
        // «12,345,678.99987» — grouped by threes, one decimal point — is the source numeral form
        // (NUMERALALPHABET), and it lexes as one number.
        const string number = "12,345,678.99987";
        Lexer ascii = new(number);
        Assert.Equal(number, (Literal.Lex(ref ascii) as Numeric)?.Memory.ToString());

        // «١» is «١», a Unicode decimal digit «char.IsDigit» would admit and the lexer does
        // not: the source alphabet is «0-9», so a run outside it is not a number at all — no
        // mixing of scripts, no lookalike that reads as a value it is not.
        Lexer unicode = new("١");
        Assert.Null(Literal.Lex(ref unicode));
    }

    [Fact(DisplayName = "the full lexer consumes a Unicode digit as a word and terminates")]
    public void TheFullLexerConsumesAUnicodeDigit()
    {
        // «١» is no numeral, but SOME token must take every character or «Lexer.Lex» spins at a
        // cursor that never advances. «Word» now admits it — a word may not START where a
        // NUMBER does, ASCII «0-9», and «١» is not a number — so a lone one is a name, the loop
        // terminates, and rejected-as-numeric does not mean rejected-by-every-token (REAUDIT65
        // finding 1). «Literal.Lex» alone never exercised this fallback.
        Assert.IsType<Word>(new Lexer("١").Lex());

        // an ASCII numeral is still a number; the Unicode digit after it is the next token, so a
        // number followed by one does not hang either.
        var after = new Lexer("5١").Lex();
        Assert.IsType<Numeric>(after);
        Assert.IsType<Word>(after.Next);

        // a Unicode digit inside a word is part of it, as it always was — only the LEADING
        // position was ever the numeral boundary.
        Assert.Equal("a١b", new Lexer("a١b").Lex().Memory.ToString());

        // and the whole pipeline terminates on one — «١» reads as an undeclared name, now its own
        // «Unresolved» finding (UNRESOLVEDRETURNRULING); reaching here at all, rather than hanging,
        // is the termination this test is about.
        Assert.IsType<Unresolved>(Assert.Single(
            Compilation.Of(new SourceText("var x => number = ١;\n", "digit.ron")).Findings));
    }
}
