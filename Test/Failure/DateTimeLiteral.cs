using Ronin.Compiler;

using Literal = Ronin.Tokens.Literals.DateTimeLiteral;

namespace Failure;

public class DateTimeLiteral
{
    [Fact(DisplayName = "not a datetime literal")]
    public void Fail()
    {
        const string literal = "not a datetime literal";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form")]
    public void BadForm()
    {
        const string literal = "1not a datetime literal";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form 2")]
    public void BadForm2()
    {
        const string literal = "12not a datetime literal";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form 3")]
    public void BadForm3()
    {
        const string literal = "123not a datetime literal";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form 4")]
    public void BadForm4()
    {
        const string literal = "1231not a datetime literal";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form 5")]
    public void BadForm5()
    {
        const string literal = "1231-not a datetime literal";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form 6")]
    public void BadForm6()
    {
        const string literal = "1231-0not a datetime literal";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form 7")]
    public void BadForm7()
    {
        const string literal = "1231-04not a datetime literal";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form 8")]
    public void BadForm8()
    {
        const string literal = "1231-04-not a datetime literal";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form 9")]
    public void BadForm9()
    {
        const string literal = "1231-02-1not a datetime literal";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form 10")]
    public void BadForm10()
    {
        const string literal = "1231-02-12not a datetime literal";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form 11")]
    public void BadForm11()
    {
        const string literal = "1231-02-12 not a datetime literal";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form 12")]
    public void BadForm12()
    {
        const string literal = "1231-02-12 0not a datetime literal";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form 13")]
    public void BadForm13()
    {
        const string literal = "1231-02-12 09not a datetime literal";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form 14")]
    public void BadForm14()
    {
        const string literal = "1231-02-12 09:not a datetime literal";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form 15")]
    public void BadForm15()
    {
        const string literal = "1231-02-12 09:1not a datetime literal";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form 16")]
    public void BadForm16()
    {
        const string literal = "1231-02-12 09:12not a datetime literal";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form 17")]
    public void BadForm17()
    {
        const string literal = "1231-02-12 09:12:not a datetime literal";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form 18")]
    public void BadForm18()
    {
        const string literal = "1231-02-12 09:12:4not a datetime literal";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form 19")]
    public void BadForm19()
    {
        const string literal = "1984-12-07 12:g4:32 p";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form 20")]
    public void BadForm20()
    {
        const string literal = "1984-12-07 12:44:32dp";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^2].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "bad form 21")]
    public void BadForm21()
    {
        const string literal = "1984-12-07 12:44:32 m";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^2].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "bad form 22")]
    public void BadForm22()
    {
        const string literal = "1984-12-07 4:44:32 m";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form 23")]
    public void BadForm23()
    {
        const string literal = "1984-12-07 2:g4:32 p";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form 24")]
    public void BadForm24()
    {
        const string literal = "1984-12-07 2:3g:32 p";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form 25")]
    public void BadForm25()
    {
        const string literal = "1984-12-07 1231-02-12 2:34?32 p";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form 26")]
    public void BadForm26()
    {
        const string literal = "1984-12-07 1231-02-12 2:34:g2 p";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form 27")]
    public void BadForm27()
    {
        const string literal = "1231-02-12 2:34:1e p";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form 28")]
    public void BadForm28()
    {
        const string literal = "1231-02-12 12:34:12vp";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^2].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "bad form 29")]
    public void BadForm29()
    {
        const string literal = "1231-02-12 12:34:12 m";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^2].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "bad form 30")]
    public void BadForm30()
    {
        const string literal = "1231-02-12 2:34:12vp";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "bad form 31")]
    public void BadForm31()
    {
        const string literal = "1231-02-12 2:34:12 m";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "too short")]
    public void TooShort()
    {
        const string literal = "1231-02-12";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Ronin.Compiler.Lexer lexer = new(string.Empty);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }
}
