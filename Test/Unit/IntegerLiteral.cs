using Ronin.Compiler;
using Ronin.Tokens.Literals;

namespace Unit;

public class IntegerLiteral
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string literal = "123_45678_90";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Literal.Integer.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length, lexed.SourceIndex);
    }

    [Fact(DisplayName = "with terminator")]
    public void WithTerminator()
    {
        const string literal = "12344567.";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Literal.Integer.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "with separator")]
    public void WithSeparator()
    {
        const string literal = "12344567,";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Literal.Integer.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "with opening parenthesis")]
    public void WithOpeningParenthesis()
    {
        const string literal = "12344567(";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Literal.Integer.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "with closing parenthesis")]
    public void WithClosingParenthesis()
    {
        const string literal = "12344567)";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Literal.Integer.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "with opening bracket")]
    public void WithOpeningBracket()
    {
        const string literal = "12344567[";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Literal.Integer.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "with closing bracket")]
    public void WithClosingBracket()
    {
        const string literal = "12344567]";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Literal.Integer.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "with opening brace")]
    public void WithOpeningBrace()
    {
        const string literal = "12344567{";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Literal.Integer.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "with closing brace")]
    public void WithClosingBrace()
    {
        const string literal = "12344567}";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Literal.Integer.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "with single quote")]
    public void WithSingleQuote()
    {
        const string literal = "12344567'";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Literal.Integer.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "with double quote")]
    public void WithDoubleQuote()
    {
        const string literal = "12344567\"";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Literal.Integer.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "with space")]
    public void WithSpace()
    {
        const string literal = "12344567 ";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Literal.Integer.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }
}
