namespace Unit;

public class Name
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string name = "thing";

        Ronin.Compiler.Lexer lexer = new(name);
        var lexed = Ronin.Token.Name.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(name.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with terminator")]
    public void WithTerminator()
    {
        const string name = "thing;";

        Ronin.Compiler.Lexer lexer = new(name);
        var lexed = Ronin.Token.Name.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(name[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with separator")]
    public void WithSeparator()
    {
        const string name = "thing,";

        Ronin.Compiler.Lexer lexer = new(name);
        var lexed = Ronin.Token.Name.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(name[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with opening parenthesis")]
    public void WithOpeningParenthesis()
    {
        const string name = "thing(";

        Ronin.Compiler.Lexer lexer = new(name);
        var lexed = Ronin.Token.Name.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(name[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with closing parenthesis")]
    public void WithClosingParenthesis()
    {
        const string name = "thing)";

        Ronin.Compiler.Lexer lexer = new(name);
        var lexed = Ronin.Token.Name.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(name[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with opening bracket")]
    public void WithOpeningBracket()
    {
        const string name = "thing[";

        Ronin.Compiler.Lexer lexer = new(name);
        var lexed = Ronin.Token.Name.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(name[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with closing bracket")]
    public void WithClosingBracket()
    {
        const string name = "thing]";

        Ronin.Compiler.Lexer lexer = new(name);
        var lexed = Ronin.Token.Name.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(name[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with opening brace")]
    public void WithOpeningBrace()
    {
        const string name = "thing{";

        Ronin.Compiler.Lexer lexer = new(name);
        var lexed = Ronin.Token.Name.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(name[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with closing brace")]
    public void WithClosingBrace()
    {
        const string name = "thing}";

        Ronin.Compiler.Lexer lexer = new(name);
        var lexed = Ronin.Token.Name.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(name[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with single quote")]
    public void WithSingleQuote()
    {
        const string name = "thing'";

        Ronin.Compiler.Lexer lexer = new(name);
        var lexed = Ronin.Token.Name.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(name[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with double quote")]
    public void WithDoubleQuote()
    {
        const string name = "thing\"";

        Ronin.Compiler.Lexer lexer = new(name);
        var lexed = Ronin.Token.Name.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(name[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with space")]
    public void WithSpace()
    {
        const string name = "thing ";

        Ronin.Compiler.Lexer lexer = new(name);
        var lexed = Ronin.Token.Name.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(name[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }
}
