using Ronin;
using Ronin.Compiler;
using Ronin.Lexicon;

namespace Failure;

[Trait(nameof(Lexer), null)]
public class Dates
{
    [Fact(DisplayName = "not a date literal")]
    public void Fail()
    {
        const string literal = "not a date literal";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "bad form")]
    public void BadForm()
    {
        const string literal = "1not a date literal";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "bad form 2")]
    public void BadForm2()
    {
        const string literal = "19not a date literal";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "bad form 3")]
    public void BadForm3()
    {
        const string literal = "198not a date literal";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "bad form 4")]
    public void BadForm4()
    {
        const string literal = "1984not a date literal";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "bad form 5")]
    public void BadForm5()
    {
        const string literal = "1984-not a date literal";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "bad form 6")]
    public void BadForm6()
    {
        const string literal = "1984-0not a date literal";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "bad form 7")]
    public void BadForm7()
    {
        const string literal = "1984-04not a date literal";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "bad form 8")]
    public void BadForm8()
    {
        const string literal = "1984-04-not a date literal";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "bad form 9")]
    public void BadForm9()
    {
        const string literal = "1984-04-1not a date literal";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "too short")]
    public void TooShort()
    {
        const string literal = "1984-01-4";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "one-digit year")]
    public void OneDigitYear()
    {
        // «5-01-01» looks like a date but the year is the only self-labelling
        // field: below four digits it is arithmetic, not «the first of January».
        const string literal = "5-01-01";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "three-digit year")]
    public void ThreeDigitYear()
    {
        // year 999 is written «0999-01-01»; three digits is still short.
        const string literal = "999-01-01";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "grouped year")]
    public void GroupedYear()
    {
        // the year is spelled in digits, not as a number, so a digit-group comma
        // does not belong to it: «1,234-01-01» is not a date.
        const string literal = "1,234-01-01";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }
}
