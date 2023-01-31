using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Literals;

namespace Failure;

[Trait("Lexer", null)]
public class NumberLiteral
{
    [Fact(DisplayName = "doesn't start with a number")]
    public void DoesntStartWithANumber()
    {
        const string literal = "g987.23";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "unterminated")]
    public void Unterminated()
    {
        const string literal = "9.";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.IsType<Number>(lexed);
        var integer = lexed as Number;
        Assert.Equal("9".ToArray(), integer.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "contains invalid chars")]
    public void Invalid()
    {
        const string literal = "9.2v5";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.IsType<Number>(lexed);
        var error = lexed as Number;
        Assert.Equal("9.2".ToArray(), error.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "contains multiple dots")]
    public void MultipleDots()
    {
        const string literal = "9.2.5";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        var number = lexed as Number;
        Assert.NotNull(number);
        Assert.Equal("9.2".ToArray(), number.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "bad commas")]
    public void BadCommas()
    {
        const string literal = "9,22.33";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        var number = lexed as Number;
        Assert.NotNull(number);
        Assert.Equal(new[] { '9' }, number.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }
}
