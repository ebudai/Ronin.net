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
        var lexed = Literal.Lex(ref lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "unterminated")]
    public void Unterminated()
    {
        const string literal = "9.";

        Lexer lexer = new(literal);
        var number = Literal.Lex(ref lexer) as Number;

        Assert.Equal(literal[..^1], number?.sourcecode.ToArray());
    }

    [Fact(DisplayName = "contains invalid chars")]
    public void Invalid()
    {
        const string literal = "9.2v5";

        Lexer lexer = new(literal);
        var number = Literal.Lex(ref lexer) as Number;

        Assert.Equal(literal[..^2], number?.sourcecode.ToArray());
    }

    [Fact(DisplayName = "contains multiple dots")]
    public void MultipleDots()
    {
        const string literal = "9.2.5";

        Lexer lexer = new(literal);
        var number = Literal.Lex(ref lexer) as Number;

        Assert.Equal(literal[..^2], number?.sourcecode.ToArray());
    }

    [Fact(DisplayName = "bad commas")]
    public void BadCommas()
    {
        const string literal = "9,22.33";

        Lexer lexer = new(literal);
        var number = Literal.Lex(ref lexer) as Number;

        Assert.Equal(literal[..1], number?.sourcecode.ToArray());
    }

    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Literal.Lex(ref lexer);

        Assert.Null(lexed);
    }
}
