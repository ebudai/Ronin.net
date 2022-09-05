namespace Unit;

public class Symbol
{
    [Fact(DisplayName = "terminal")]
    public void Terminal()
    {
        const string sourcecode = ";";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        var lexed = Ronin.Token.Symbol.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(sourcecode.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "separator")]
    public void Separator()
    {
        const string sourcecode = ",";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        var lexed = Ronin.Token.Symbol.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(sourcecode.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "open brace")]
    public void OpenBrace()
    {
        const string sourcecode = "{";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        var lexed = Ronin.Token.Symbol.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(sourcecode.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "open parenthesis")]
    public void OpenParenthesis()
    {
        const string sourcecode = "(";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        var lexed = Ronin.Token.Symbol.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(sourcecode.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "open square bracket")]
    public void OpenSquareBracket()
    {
        const string sourcecode = "[";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        var lexed = Ronin.Token.Symbol.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(sourcecode.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "close brace")]
    public void CloseBrace()
    {
        const string sourcecode = "}";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        var lexed = Ronin.Token.Symbol.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(sourcecode.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "close parenthesis")]
    public void CloseParenthesis()
    {
        const string sourcecode = ")";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        var lexed = Ronin.Token.Symbol.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(sourcecode.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "close square bracket")]
    public void CloseSquareBracket()
    {
        const string sourcecode = "]";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        var lexed = Ronin.Token.Symbol.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(sourcecode.ToArray(), lexed.Sourcecode.ToArray());
    }
}
