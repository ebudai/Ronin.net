using Ronin.Compiler;
using Ronin.Tokens;

namespace Unit;

public class BinaryLiteralUnitTests
{
    [Fact(DisplayName = "parse basic binary literal")]
    public void Basic()
    {
        const string literal = "0b101101_00101";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = BinaryLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length, lexed.SourceIndex);
    }

    [Fact(DisplayName = "lex binary literal with terminator")]
    public void WithTerminator()
    {
        const string literal = "0B10010.";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = BinaryLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "lex binary literal with separator")]
    public void WithSeparator()
    {
        const string literal = "0B10010,";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = BinaryLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "lex binary literal with opening parenthesis")]
    public void WithOpeningParenthesis()
    {
        const string literal = "0B10010(";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = BinaryLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "lex binary literal with closing parenthesis")]
    public void WithClosingParenthesis()
    {
        const string literal = "0B10010)";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = BinaryLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "lex binary literal with bracket")]
    public void WithOpeningBracket()
    {
        const string literal = "0B10010[";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = BinaryLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "lex binary literal with closing bracket")]
    public void WithClosingBracket()
    {
        const string literal = "0B10010]";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = BinaryLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "lex binary literal with brace")]
    public void WithOpeningBrace()
    {
        const string literal = "0B10010{";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = BinaryLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "lex binary literal with closing brace")]
    public void WithClosingBrace()
    {
        const string literal = "0B10010}";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = BinaryLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "lex binary literal with single quote")]
    public void WithSingleQuote()
    {
        const string literal = "0B10010'";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = BinaryLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "lex binary literal with double quote")]
    public void WithDoubleQuote()
    {
        const string literal = "0B10010\"";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = BinaryLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }

    [Fact(DisplayName = "lex binary literal with space")]
    public void WithSpace()
    {
        const string literal = "0B10010 ";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = BinaryLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length - 1, lexed.SourceIndex);
    }
}
