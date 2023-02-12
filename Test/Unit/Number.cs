using Ronin.Compiler;
using Ronin.Lexicon;

namespace Unit;

[Trait("Lexer", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class number
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string literal = "123,456.7890";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.Equal(literal.ToArray(), lexed?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with terminator")]
    public void WithTerminator()
    {
        const string literal = "1234.4567;";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.Equal(literal[..^1].ToArray(), lexed?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with separator")]
    public void WithSeparator()
    {
        const string literal = "1234.4567,";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.Equal(literal[..^1].ToArray(), lexed?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with opening parenthesis")]
    public void WithOpeningParenthesis()
    {
        const string literal = "1234.4567(";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.Equal(literal[..^1].ToArray(), lexed?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with closing parenthesis")]
    public void WithClosingParenthesis()
    {
        const string literal = "1234.4567)";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.Equal(literal[..^1].ToArray(), lexed?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with opening bracket")]
    public void WithOpeningBracket()
    {
        const string literal = "1234.4567[";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.Equal(literal[..^1].ToArray(), lexed?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with closing bracket")]
    public void WithClosingBracket()
    {
        const string literal = "1234.4567]";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.Equal(literal[..^1].ToArray(), lexed?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with opening brace")]
    public void WithOpeningBrace()
    {
        const string literal = "1234.4567{";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.Equal(literal[..^1].ToArray(), lexed?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with closing brace")]
    public void WithClosingBrace()
    {
        const string literal = "1234.4567}";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.Equal(literal[..^1].ToArray(), lexed?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with single quote")]
    public void WithSingleQuote()
    {
        const string literal = "1234.4567'";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.Equal(literal[..^1].ToArray(), lexed?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with double quote")]
    public void WithDoubleQuote()
    {
        const string literal = "1234.4567\"";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.Equal(literal[..^1].ToArray(), lexed?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with space")]
    public void WithSpace()
    {
        const string literal = "1234.4567 ";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.Equal(literal[..^1].ToArray(), lexed?.Sourcecode.ToArray());
    }
}
