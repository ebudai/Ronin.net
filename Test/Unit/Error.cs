namespace Unit;

public class Error
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string error = "4werrwe";

        Ronin.Compiler.Lexer lexer = new(error);
        var lexed = Ronin.Token.Error.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(error.Length, lexed.Sourcecode.Length);
    }

    [Fact(DisplayName = "with terminator")]
    public void WithTerminator()
    {
        const string error = "4werrwe;";

        Ronin.Compiler.Lexer lexer = new(error);
        var lexed = Ronin.Token.Error.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(error[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with separator")]
    public void WithSeparator()
    {
        const string error = "4werrwe,";

        Ronin.Compiler.Lexer lexer = new(error);
        var lexed = Ronin.Token.Error.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(error[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with opening parenthesis")]
    public void WithOpeningParenthesis()
    {
        const string error = "4werrwe(";

        Ronin.Compiler.Lexer lexer = new(error);
        var lexed = Ronin.Token.Error.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(error[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with closing parenthesis")]
    public void WithClosingParenthesis()
    {
        const string error = "4werrwe)";

        Ronin.Compiler.Lexer lexer = new(error);
        var lexed = Ronin.Token.Error.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(error[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with opening bracket")]
    public void WithOpeningBracket()
    {
        const string error = "4werrwe[";

        Ronin.Compiler.Lexer lexer = new(error);
        var lexed = Ronin.Token.Error.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(error[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with closing bracket")]
    public void WithClosingBracket()
    {
        const string error = "4werrwe]";

        Ronin.Compiler.Lexer lexer = new(error);
        var lexed = Ronin.Token.Error.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(error[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with opening brace")]
    public void WithOpeningBrace()
    {
        const string error = "4werrwe{";

        Ronin.Compiler.Lexer lexer = new(error);
        var lexed = Ronin.Token.Error.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(error[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with closing brace")]
    public void WithClosingBrace()
    {
        const string error = "4werrwe}";

        Ronin.Compiler.Lexer lexer = new(error);
        var lexed = Ronin.Token.Error.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(error[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with single quote")]
    public void WithSingleQuote()
    {
        const string error = "4werrwe'";

        Ronin.Compiler.Lexer lexer = new(error);
        var lexed = Ronin.Token.Error.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(error[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with double quote")]
    public void WithDoubleQuote()
    {
        const string error = "4werrwe\"";

        Ronin.Compiler.Lexer lexer = new(error);
        var lexed = Ronin.Token.Error.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(error[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with space")]
    public void WithSpace()
    {
        const string error = "4werrwe ";

        Ronin.Compiler.Lexer lexer = new(error);
        var lexed = Ronin.Token.Error.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(error[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }
}
