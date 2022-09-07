namespace Unit;

public class Symbol
{
    [Fact(DisplayName = "terminal")]
    public void Terminal()
    {
        const string sourcecode = ";";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        Assert.True(Ronin.Token.Symbol.IsSymbol(lexer));
        var lexed = Ronin.Token.Symbol.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(sourcecode.ToArray(), lexed.Sourcecode.ToArray());
        
        Assert.IsType<Ronin.Token.Symbol>(lexed);
        var symbol = lexed as Ronin.Token.Symbol;
        
        Assert.True(symbol.IsTerminal);
        Assert.False(symbol.IsCharacterDelimiter);
        Assert.False(symbol.IsCloseBrace);
        Assert.False(symbol.IsCloseParenthesis);
        Assert.False(symbol.IsCloseSquareBracket);
        Assert.False(symbol.IsOpenBrace);
        Assert.False(symbol.IsOpenParenthesis);
        Assert.False(symbol.IsOpenSquareBracket);
        Assert.False(symbol.IsReturns);
        Assert.False(symbol.IsSeparator);
        Assert.False(symbol.IsTextDelimiter);
    }

    [Fact(DisplayName = "separator")]
    public void Separator()
    {
        const string sourcecode = ",";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        Assert.True(Ronin.Token.Symbol.IsSymbol(lexer));
        var lexed = Ronin.Token.Symbol.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(sourcecode.ToArray(), lexed.Sourcecode.ToArray());
        
        Assert.IsType<Ronin.Token.Symbol>(lexed);
        var symbol = lexed as Ronin.Token.Symbol;
        
        Assert.True(symbol.IsSeparator);
        Assert.False(symbol.IsCharacterDelimiter);
        Assert.False(symbol.IsCloseBrace);
        Assert.False(symbol.IsCloseParenthesis);
        Assert.False(symbol.IsCloseSquareBracket);
        Assert.False(symbol.IsOpenBrace);
        Assert.False(symbol.IsOpenParenthesis);
        Assert.False(symbol.IsOpenSquareBracket);
        Assert.False(symbol.IsReturns);
        Assert.False(symbol.IsTerminal);
        Assert.False(symbol.IsTextDelimiter);
    }

    [Fact(DisplayName = "open brace")]
    public void OpenBrace()
    {
        const string sourcecode = "{";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        Assert.True(Ronin.Token.Symbol.IsSymbol(lexer));
        var lexed = Ronin.Token.Symbol.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(sourcecode.ToArray(), lexed.Sourcecode.ToArray());
        
        Assert.IsType<Ronin.Token.Symbol>(lexed);
        var symbol = lexed as Ronin.Token.Symbol;
        
        Assert.True(symbol.IsOpenBrace);
        Assert.False(symbol.IsCharacterDelimiter);
        Assert.False(symbol.IsCloseBrace);
        Assert.False(symbol.IsCloseParenthesis);
        Assert.False(symbol.IsCloseSquareBracket);        
        Assert.False(symbol.IsOpenParenthesis);
        Assert.False(symbol.IsOpenSquareBracket);
        Assert.False(symbol.IsReturns);
        Assert.False(symbol.IsSeparator);
        Assert.False(symbol.IsTerminal);
        Assert.False(symbol.IsTextDelimiter);
    }

    [Fact(DisplayName = "open parenthesis")]
    public void OpenParenthesis()
    {
        const string sourcecode = "(";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        Assert.True(Ronin.Token.Symbol.IsSymbol(lexer));
        var lexed = Ronin.Token.Symbol.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(sourcecode.ToArray(), lexed.Sourcecode.ToArray());
        
        Assert.IsType<Ronin.Token.Symbol>(lexed);
        var symbol = lexed as Ronin.Token.Symbol;
        
        Assert.True(symbol.IsOpenParenthesis);
        Assert.False(symbol.IsCharacterDelimiter);
        Assert.False(symbol.IsCloseBrace);
        Assert.False(symbol.IsCloseParenthesis);
        Assert.False(symbol.IsCloseSquareBracket);
        Assert.False(symbol.IsOpenBrace);
        Assert.False(symbol.IsOpenSquareBracket);
        Assert.False(symbol.IsReturns);
        Assert.False(symbol.IsSeparator);
        Assert.False(symbol.IsTerminal);
        Assert.False(symbol.IsTextDelimiter);
    }

    [Fact(DisplayName = "open square bracket")]
    public void OpenSquareBracket()
    {
        const string sourcecode = "[";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        Assert.True(Ronin.Token.Symbol.IsSymbol(lexer));
        var lexed = Ronin.Token.Symbol.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(sourcecode.ToArray(), lexed.Sourcecode.ToArray());
        
        Assert.IsType<Ronin.Token.Symbol>(lexed);
        var symbol = lexed as Ronin.Token.Symbol;
        
        Assert.True(symbol.IsOpenSquareBracket);
        Assert.False(symbol.IsCharacterDelimiter);
        Assert.False(symbol.IsCloseBrace);
        Assert.False(symbol.IsCloseParenthesis);
        Assert.False(symbol.IsCloseSquareBracket);
        Assert.False(symbol.IsOpenBrace);
        Assert.False(symbol.IsOpenParenthesis);
        Assert.False(symbol.IsReturns);
        Assert.False(symbol.IsSeparator);
        Assert.False(symbol.IsTerminal);
        Assert.False(symbol.IsTextDelimiter);
    }

    [Fact(DisplayName = "close brace")]
    public void CloseBrace()
    {
        const string sourcecode = "}";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        Assert.True(Ronin.Token.Symbol.IsSymbol(lexer));
        var lexed = Ronin.Token.Symbol.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(sourcecode.ToArray(), lexed.Sourcecode.ToArray());
        
        Assert.IsType<Ronin.Token.Symbol>(lexed);
        var symbol = lexed as Ronin.Token.Symbol;
        
        Assert.True(symbol.IsCloseBrace);
        Assert.False(symbol.IsCharacterDelimiter);        
        Assert.False(symbol.IsCloseParenthesis);
        Assert.False(symbol.IsCloseSquareBracket);
        Assert.False(symbol.IsOpenBrace);
        Assert.False(symbol.IsOpenParenthesis);
        Assert.False(symbol.IsOpenSquareBracket);
        Assert.False(symbol.IsReturns);
        Assert.False(symbol.IsSeparator);
        Assert.False(symbol.IsTerminal);
        Assert.False(symbol.IsTextDelimiter);
    }

    [Fact(DisplayName = "close parenthesis")]
    public void CloseParenthesis()
    {
        const string sourcecode = ")";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        Assert.True(Ronin.Token.Symbol.IsSymbol(lexer));
        var lexed = Ronin.Token.Symbol.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(sourcecode.ToArray(), lexed.Sourcecode.ToArray());
        
        Assert.IsType<Ronin.Token.Symbol>(lexed);
        var symbol = lexed as Ronin.Token.Symbol;
        
        Assert.True(symbol.IsCloseParenthesis);
        Assert.False(symbol.IsCharacterDelimiter);
        Assert.False(symbol.IsCloseBrace);
        Assert.False(symbol.IsCloseSquareBracket);
        Assert.False(symbol.IsOpenBrace);
        Assert.False(symbol.IsOpenParenthesis);
        Assert.False(symbol.IsOpenSquareBracket);
        Assert.False(symbol.IsReturns);
        Assert.False(symbol.IsSeparator);
        Assert.False(symbol.IsTerminal);
        Assert.False(symbol.IsTextDelimiter);
    }

    [Fact(DisplayName = "close square bracket")]
    public void CloseSquareBracket()
    {
        const string sourcecode = "]";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        Assert.True(Ronin.Token.Symbol.IsSymbol(lexer));
        var lexed = Ronin.Token.Symbol.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(sourcecode.ToArray(), lexed.Sourcecode.ToArray());
        
        Assert.IsType<Ronin.Token.Symbol>(lexed);
        var symbol = lexed as Ronin.Token.Symbol;
        
        Assert.True(symbol.IsCloseSquareBracket);
        Assert.False(symbol.IsCharacterDelimiter);
        Assert.False(symbol.IsCloseBrace);
        Assert.False(symbol.IsCloseParenthesis);
        Assert.False(symbol.IsOpenBrace);
        Assert.False(symbol.IsOpenParenthesis);
        Assert.False(symbol.IsOpenSquareBracket);
        Assert.False(symbol.IsReturns);
        Assert.False(symbol.IsSeparator);
        Assert.False(symbol.IsTerminal);
        Assert.False(symbol.IsTextDelimiter);
    }

    [Fact(DisplayName = "single quote")]
    public void SingleQuote()
    {
        const string sourcecode = "'";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        Assert.True(Ronin.Token.Symbol.IsSymbol(lexer));
        var lexed = Ronin.Token.Symbol.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(sourcecode.ToArray(), lexed.Sourcecode.ToArray());
        Assert.IsType<Ronin.Token.Symbol>(lexed);
        var symbol = lexed as Ronin.Token.Symbol;
        
        Assert.True(symbol.IsCharacterDelimiter);
        Assert.False(symbol.IsCloseBrace);
        Assert.False(symbol.IsCloseParenthesis);
        Assert.False(symbol.IsCloseSquareBracket);
        Assert.False(symbol.IsOpenBrace);
        Assert.False(symbol.IsOpenParenthesis);
        Assert.False(symbol.IsOpenSquareBracket);
        Assert.False(symbol.IsReturns);
        Assert.False(symbol.IsSeparator);
        Assert.False(symbol.IsTerminal);
        Assert.False(symbol.IsTextDelimiter);
    }

    [Fact(DisplayName = "double quote")]
    public void DoubleQuote()
    {
        const string sourcecode = "\"";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        Assert.True(Ronin.Token.Symbol.IsSymbol(lexer));
        var lexed = Ronin.Token.Symbol.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(sourcecode.ToArray(), lexed.Sourcecode.ToArray());
        
        Assert.IsType<Ronin.Token.Symbol>(lexed);
        var symbol = lexed as Ronin.Token.Symbol;
        
        Assert.True(symbol.IsTextDelimiter);
        Assert.False(symbol.IsCharacterDelimiter);
        Assert.False(symbol.IsCloseBrace);
        Assert.False(symbol.IsCloseParenthesis);
        Assert.False(symbol.IsCloseSquareBracket);
        Assert.False(symbol.IsOpenBrace);
        Assert.False(symbol.IsOpenParenthesis);
        Assert.False(symbol.IsOpenSquareBracket);
        Assert.False(symbol.IsReturns);
        Assert.False(symbol.IsSeparator);
        Assert.False(symbol.IsTerminal);
    }

    [Fact(DisplayName = "returns")]
    public void Returns()
    {
        const string sourcecode = "=>";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        Assert.True(Ronin.Token.Symbol.IsSymbol(lexer));
        var lexed = Ronin.Token.Symbol.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(sourcecode.ToArray(), lexed.Sourcecode.ToArray());
        
        Assert.IsType<Ronin.Token.Symbol>(lexed);
        var symbol = lexed as Ronin.Token.Symbol;
        
        Assert.True(symbol.IsReturns);
        Assert.False(symbol.IsCharacterDelimiter);
        Assert.False(symbol.IsCloseBrace);
        Assert.False(symbol.IsCloseParenthesis);
        Assert.False(symbol.IsCloseSquareBracket);
        Assert.False(symbol.IsOpenBrace);
        Assert.False(symbol.IsOpenParenthesis);
        Assert.False(symbol.IsOpenSquareBracket);
        Assert.False(symbol.IsSeparator);
        Assert.False(symbol.IsTerminal);
        Assert.False(symbol.IsTextDelimiter);
    }
}
