using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Literals;

namespace Unit;

//todo money should use commas instead of underscores - look at Number.Lex(...)

[Trait("Lexer", null)]
public class MoneyLiteral
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string literal = "$123_456.78_90";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as Money;

        Assert.Equal(literal, money?.sourcecode.ToArray());
    }

    [Fact(DisplayName = "with terminator")]
    public void WithTerminator()
    {
        const string literal = "$1234.4567;";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as Money;

        Assert.Equal(literal[..^1], money?.sourcecode.ToArray());
    }

    [Fact(DisplayName = "with separator")]
    public void WithSeparator()
    {
        const string literal = "$1234.4567,";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as Money;

        Assert.Equal(literal[..^1], money?.sourcecode.ToArray());
    }

    [Fact(DisplayName = "with opening parenthesis")]
    public void WithOpeningParenthesis()
    {
        const string literal = "$1234.4567(";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as Money;

        Assert.Equal(literal[..^1], money?.sourcecode.ToArray());
    }

    [Fact(DisplayName = "with closing parenthesis")]
    public void WithClosingParenthesis()
    {
        const string literal = "$1234.4567)";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as Money;

        Assert.Equal(literal[..^1], money?.sourcecode.ToArray());
    }

    [Fact(DisplayName = "with opening bracket")]
    public void WithOpeningBracket()
    {
        const string literal = "$1234.4567[";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as Money;

        Assert.Equal(literal[..^1], money?.sourcecode.ToArray());
    }

    [Fact(DisplayName = "with closing bracket")]
    public void WithClosingBracket()
    {
        const string literal = "$1234.4567]";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as Money;

        Assert.Equal(literal[..^1], money?.sourcecode.ToArray());
    }

    [Fact(DisplayName = "with opening brace")]
    public void WithOpeningBrace()
    {
        const string literal = "$1234.4567{";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as Money;

        Assert.Equal(literal[..^1], money?.sourcecode.ToArray());
    }

    [Fact(DisplayName = "with closing brace")]
    public void WithClosingBrace()
    {
        const string literal = "$1234.4567}";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as Money;

        Assert.Equal(literal[..^1], money?.sourcecode.ToArray());
    }

    [Fact(DisplayName = "with single quote")]
    public void WithSingleQuote()
    {
        const string literal = "$1234.4567'";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as Money;

        Assert.Equal(literal[..^1], money?.sourcecode.ToArray());
    }

    [Fact(DisplayName = "with double quote")]
    public void WithDoubleQuote()
    {
        const string literal = "$1234.4567\"";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as Money;

        Assert.Equal(literal[..^1], money?.sourcecode.ToArray());
    }

    [Fact(DisplayName = "with space")]
    public void WithSpace()
    {
        const string literal = "$1234.4567 ";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as Money;

        Assert.Equal(literal[..^1], money?.sourcecode.ToArray());
    }

    [Fact(DisplayName = "whole value")]
    public void Whole()
    {
        const string literal = "$1234";

        Lexer lexer = new(literal);
        var money = Literal.Lex(ref lexer) as Money;

        Assert.Equal(literal, money?.sourcecode.ToArray());
    }
}
