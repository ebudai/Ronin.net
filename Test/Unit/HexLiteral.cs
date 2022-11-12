using Ronin.Lexicon;
using Ronin.Lexicon.Literals;

namespace Unit;

[Trait("Lexer", null)]
public class HexLiteral
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string literal = "0x1234_5678_90AB_CDEF_abcdef";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with terminator")]
    public void WithTerminator()
    {
        const string literal = "0X1212;";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with separator")]
    public void WithSeparator()
    {
        const string literal = "0x1212,";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with opening parenthesis")]
    public void WithOpeningParenthesis()
    {
        const string literal = "0X1212(";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with closing parenthesis")]
    public void WithClosingParenthesis()
    {
        const string literal = "0x1212)";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with opening bracket")]
    public void WithOpeningBracket()
    {
        const string literal = "0x1212[";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with closing bracket")]
    public void WithClosingBracket()
    {
        const string literal = "0X1212]";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with opening brace")]
    public void WithOpeningBrace()
    {
        const string literal = "0X1212{";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with closing brace")]
    public void WithClosingBrace()
    {
        const string literal = "0x1212}";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with single quote")]
    public void WithSingleQuote()
    {
        const string literal = "0X1212'";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with double quote")]
    public void WithDoubleQuote()
    {
        const string literal = "0x1212\"";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with space")]
    public void WithSpace()
    {
        const string literal = "0x1212 ";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "contains invalid chars")]
    public void Invalid()
    {
        const string literal = "0x1234g";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.IsType<Hex>(lexed);
        var hex = lexed as Hex;
        Assert.Equal(literal[..^1].ToArray(), hex.Sourcecode.ToArray());
    }
}
