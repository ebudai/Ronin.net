using Ronin;
using Ronin.Compiler;
using Ronin.Lexicon;

namespace Unit;

//todo money should use commas instead of underscores - look at Number.Lex(...)

[Trait("Lexer", null)]
public class Money
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string literal = "$123_456.78_90";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as MoneyLiteral;

        Assert.Equal(literal, money?.ToString());
    }

    [Fact(DisplayName = "with terminator")]
    public void WithTerminator()
    {
        const string literal = "$1234.4567;";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as MoneyLiteral;

        Assert.Equal(literal[..^1], money?.ToString());
    }

    [Fact(DisplayName = "with separator")]
    public void WithSeparator()
    {
        const string literal = "$1234.4567,";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as MoneyLiteral;

        Assert.Equal(literal[..^1], money?.ToString());
    }

    [Fact(DisplayName = "with opening parenthesis")]
    public void WithOpeningParenthesis()
    {
        const string literal = "$1234.4567(";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as MoneyLiteral;

        Assert.Equal(literal[..^1], money?.ToString());
    }

    [Fact(DisplayName = "with closing parenthesis")]
    public void WithClosingParenthesis()
    {
        const string literal = "$1234.4567)";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as MoneyLiteral;

        Assert.Equal(literal[..^1], money?.ToString());
    }

    [Fact(DisplayName = "with opening bracket")]
    public void WithOpeningBracket()
    {
        const string literal = "$1234.4567[";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as MoneyLiteral;

        Assert.Equal(literal[..^1], money?.ToString());
    }

    [Fact(DisplayName = "with closing bracket")]
    public void WithClosingBracket()
    {
        const string literal = "$1234.4567]";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as MoneyLiteral;

        Assert.Equal(literal[..^1], money?.ToString());
    }

    [Fact(DisplayName = "with opening brace")]
    public void WithOpeningBrace()
    {
        const string literal = "$1234.4567{";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as MoneyLiteral;

        Assert.Equal(literal[..^1], money?.ToString());
    }

    [Fact(DisplayName = "with closing brace")]
    public void WithClosingBrace()
    {
        const string literal = "$1234.4567}";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as MoneyLiteral;

        Assert.Equal(literal[..^1], money?.ToString());
    }

    [Fact(DisplayName = "with single quote")]
    public void WithSingleQuote()
    {
        const string literal = "$1234.4567'";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as MoneyLiteral;

        Assert.Equal(literal[..^1], money?.ToString());
    }

    [Fact(DisplayName = "with double quote")]
    public void WithDoubleQuote()
    {
        const string literal = "$1234.4567\"";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as MoneyLiteral;

        Assert.Equal(literal[..^1], money?.ToString());
    }

    [Fact(DisplayName = "with space")]
    public void WithSpace()
    {
        const string literal = "$1234.4567 ";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as MoneyLiteral;

        Assert.Equal(literal[..^1], money?.ToString());
    }

    [Fact(DisplayName = "whole value")]
    public void Whole()
    {
        const string literal = "$1234";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as MoneyLiteral;

        Assert.Equal(literal, money?.ToString());
    }
}
