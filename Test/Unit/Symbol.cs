using Ronin.Token.Symbols;

namespace Unit;

public class Symbol
{
    private static void LexSymbol<T>(string lexed) where T : Ronin.Token.Symbol
    {
        Ronin.Compiler.Lexer lexer = new(lexed);
        Assert.True(Ronin.Token.Symbol.IsSymbol(lexer));
        var symbol = Ronin.Token.Symbol.Lex(lexer);

        Assert.NotNull(symbol);
        Assert.Equal(lexed.ToArray(), symbol.Sourcecode.ToArray());

        Assert.IsType<T>(symbol);
    }

    [Fact(DisplayName = "terminal")]
    public void LexTerminal() => LexSymbol<Terminal>(Terminal.character.ToString());

    [Fact(DisplayName = "separator")]
    public void LexSeparator() => LexSymbol<Separator>(Separator.character.ToString());

    [Fact(DisplayName = "open brace")]
    public void LexOpenBrace() => LexSymbol<OpenBrace>(OpenBrace.character.ToString());

    [Fact(DisplayName = "open parenthesis")]
    public void LexOpenParenthesis() => LexSymbol<OpenParenthesis>(OpenParenthesis.character.ToString());

    [Fact(DisplayName = "open square bracket")]
    public void LexOpenSquareBracket() => LexSymbol<OpenSquareBracket>(OpenSquareBracket.character.ToString());

    [Fact(DisplayName = "close brace")]
    public void LexCloseBrace() => LexSymbol<CloseBrace>(CloseBrace.character.ToString());

    [Fact(DisplayName = "close parenthesis")]
    public void LexCloseParenthesis() => LexSymbol<CloseParenthesis>(CloseParenthesis.character.ToString());

    [Fact(DisplayName = "close square bracket")]
    public void LexCloseSquareBracket() => LexSymbol<CloseSquareBracket>(CloseSquareBracket.character.ToString());

    [Fact(DisplayName = "single quote")]
    public void LexSingleQuote() => LexSymbol<CharacterDelimiter>(CharacterDelimiter.character.ToString());

    [Fact(DisplayName = "double quote")]
    public void LexDoubleQuote() => LexSymbol<TextDelimiter>(TextDelimiter.character.ToString());

    [Fact(DisplayName = "returns")]
    public void LexReturns() => LexSymbol<Returns>(Returns.character);

    [Fact(DisplayName = "assign")]
    public void LexAssign() => LexSymbol<Assign>(Assign.character.ToString());
}
