using Ronin.Compiler;
using Ronin.Tokens;

namespace Unit;

public class HexLiteralUnitTests
{
    [Fact(DisplayName = "parse basic hex literal")]
    public void Basic()
    {
        const string literal = "0x1234_5678_90AB_CDEF_abcdef";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = HexLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length, lexed.SourceIndex);
    }

    [Fact(DisplayName = "lex hex literal with terminator")]
    public void WithTerminator()
    {
        const string literal = "0X1212.";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = HexLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "lex hex literal with separator")]
    public void WithSeparator()
    {
        const string literal = "0x1212,";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = HexLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "lex hex literal with opening parenthesis")]
    public void WithOpeningParenthesis()
    {
        const string literal = "0X1212(";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = HexLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "lex hex literal with closing parenthesis")]
    public void WithClosingParenthesis()
    {
        const string literal = "0x1212)";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = HexLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "lex hex literal with bracket")]
    public void WithOpeningBracket()
    {
        const string literal = "0x1212[";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = HexLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "lex hex literal with closing bracket")]
    public void WithClosingBracket()
    {
        const string literal = "0X1212]";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = HexLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "lex hex literal with brace")]
    public void WithOpeningBrace()
    {
        const string literal = "0X1212{";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = HexLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "lex hex literal with closing brace")]
    public void WithClosingBrace()
    {
        const string literal = "0x1212}";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = HexLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "lex hex literal with single quote")]
    public void WithSingleQuote()
    {
        const string literal = "0X1212'";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = HexLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "lex hex literal with double quote")]
    public void WithDoubleQuote()
    {
        const string literal = "0x1212\"";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = HexLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "lex hex literal with space")]
    public void WithSpace()
    {
        const string literal = "0x1212 ";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = HexLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }
}
