using Ronin;
using Ronin.Compiler;
using Ronin.Lexicon;

namespace Failure;

[Trait("Lexer", null)]
public class Time
{
    [Fact(DisplayName = "not a time literal")]
    public void Fail()
    {
        const string literal = "not a time literal";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "bad form")]
    public void BadForm()
    {
        const string literal = "1not a time literal";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "bad form 2")]
    public void BadForm2()
    {
        const string literal = "12not a time literal";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "bad form 3")]
    public void BadForm3()
    {
        const string literal = "12:0not a time literal";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "bad form 4")]
    public void BadForm4()
    {
        const string literal = "12:04not a time literal";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "bad form 5")]
    public void BadForm5()
    {
        const string literal = "12:04:not a time literal";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "bad form 6")]
    public void BadForm6()
    {
        const string literal = "12:04:3not a time literal";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "bad form 7")]
    public void BadForm7()
    {
        const string literal = "12:g4:32 p";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "bad form 8")]
    public void BadForm8()
    {
        const string literal = "12:44:32dp";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^2].ToArray(), lexed.Memory.ToArray());
    }

    [Fact(DisplayName = "bad form 9")]
    public void BadForm9()
    {
        const string literal = "12:44:32 m";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^2].ToArray(), lexed.Memory.ToArray());
    }

    [Fact(DisplayName = "bad form 10")]
    public void BadForm10()
    {
        const string literal = "2:g4:32 p";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "bad form 11")]
    public void BadForm11()
    {
        const string literal = "2:3g:32 p";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "bad form 12")]
    public void BadForm12()
    {
        const string literal = "2:34?32 p";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "bad form 13")]
    public void BadForm13()
    {
        const string literal = "2:34:g2 p";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "bad form 14")]
    public void BadForm14()
    {
        const string literal = "2:34:1e p";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "bad form 15")]
    public void BadForm15()
    {
        const string literal = "2:34:12vp";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "bad form 16")]
    public void BadForm16()
    {
        const string literal = "2:34:12 m";

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
