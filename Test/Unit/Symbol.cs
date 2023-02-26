using Ronin.Compiler;
using Ronin.Lexicon;

namespace Unit;

[Trait("Lexer", null)]
public class Symbol
{
    private static void LexSymbol<T>(string lexed) where T : Ronin.Lexicon.Symbol
    {
        Lexer lexer = new(lexed);
        Assert.True(Ronin.Lexicon.Symbol.IsSymbol(ref lexer));
        var symbol = Ronin.Lexicon.Symbol.Lex(ref lexer);

        Assert.Equal(lexed.ToArray(), symbol?.sourcecode.ToArray());

        Assert.IsType<T>(symbol);
    }

    [Fact(DisplayName = "terminal")]
    public void LexTerminal() => LexSymbol<TerminalSymbol>(TerminalSymbol.symbol);

    [Fact(DisplayName = "separator")]
    public void LexSeparator() => LexSymbol<SeparatorSymbol>(SeparatorSymbol.symbol);

    [Fact(DisplayName = "open brace")]
    public void LexOpenBrace() => LexSymbol<OpenBraceSymbol>(OpenBraceSymbol.symbol);

    [Fact(DisplayName = "open parenthesis")]
    public void LexOpenParenthesis() => LexSymbol<OpenParenthesisSymbol>(OpenParenthesisSymbol.symbol);

    [Fact(DisplayName = "open square bracket")]
    public void LexOpenSquareBracket() => LexSymbol<OpenSquareBracketSymbol>(OpenSquareBracketSymbol.symbol);

    [Fact(DisplayName = "close brace")]
    public void LexCloseBrace() => LexSymbol<CloseBraceSymbol>(CloseBraceSymbol.symbol);

    [Fact(DisplayName = "close parenthesis")]
    public void LexCloseParenthesis() => LexSymbol<CloseParenthesisSymbol>(CloseParenthesisSymbol.symbol);

    [Fact(DisplayName = "close square bracket")]
    public void LexCloseSquareBracket() => LexSymbol<CloseSquareBracketSymbol>(CloseSquareBracketSymbol.symbol);

    [Fact(DisplayName = "character delimiter")]
    public void LexSingleQuote() => LexSymbol<CharacterDelimiterSymbol>(CharacterDelimiterSymbol.symbol);

    [Fact(DisplayName = "text delimiter")]
    public void LexDoubleQuote() => LexSymbol<TextDelimiterSymbol>(TextDelimiterSymbol.symbol);

    [Fact(DisplayName = "returns")]
    public void LexReturns() => LexSymbol<ReturnsSymbol>(ReturnsSymbol.symbol);

    [Fact(DisplayName = "assign")]
    public void LexAssign() => LexSymbol<AssignSymbol>(AssignSymbol.symbol);

    [Fact(DisplayName = "ampersand")]
    public void LexAmpersand() => LexSymbol<AmpersandSymbol>(AmpersandSymbol.symbol);

    [Fact(DisplayName = "asterisk")]
    public void LexAsterisk() => LexSymbol<AsteriskSymbol>(AsteriskSymbol.symbol);

    [Fact(DisplayName = "at")]
    public void LexAt() => LexSymbol<AtSymbol>(AtSymbol.symbol);

    [Fact(DisplayName = "backslash")]
    public void LexBackslash() => LexSymbol<BackslashSymbol>(BackslashSymbol.symbol);

    [Fact(DisplayName = "chevron")]
    public void LexChevron() => LexSymbol<ChevronSymbol>(ChevronSymbol.symbol);

    [Fact(DisplayName = "colon")]
    public void LexColon() => LexSymbol<ColonSymbol>(ColonSymbol.symbol);

    [Fact(DisplayName = "dollar")]
    public void LexDollar() => LexSymbol<DollarSymbol>(DollarSymbol.symbol);

    [Fact(DisplayName = "exclamation")]
    public void LexExclamation() => LexSymbol<ExclamationSymbol>(ExclamationSymbol.symbol);

    [Fact(DisplayName = "greater than")]
    public void LexGreaterThan() => LexSymbol<GreaterThanSymbol>(GreaterThanSymbol.symbol);

    [Fact(DisplayName = "less than")]
    public void LexLessThan() => LexSymbol<LessThanSymbol>(LessThanSymbol.symbol);

    [Fact(DisplayName = "minus")]
    public void LexMinus() => LexSymbol<MinusSymbol>(MinusSymbol.symbol);

    [Fact(DisplayName = "percent")]
    public void LexPercent() => LexSymbol<PercentSymbol>(PercentSymbol.symbol);

    [Fact(DisplayName = "pipe")]
    public void LexPipe() => LexSymbol<PipeSymbol>(PipeSymbol.symbol);

    [Fact(DisplayName = "plus")]
    public void LexPlus() => LexSymbol<PlusSymbol>(PlusSymbol.symbol);

    [Fact(DisplayName = "pound")]
    public void LexPound() => LexSymbol<PoundSymbol>(PoundSymbol.symbol);

    [Fact(DisplayName = "question")]
    public void LexQuestion() => LexSymbol<QuestionSymbol>(QuestionSymbol.symbol);

    [Fact(DisplayName = "slash")]
    public void LexSlash() => LexSymbol<SlashSymbol>(SlashSymbol.symbol);

    [Fact(DisplayName = "tilde")]
    public void LexTilde() => LexSymbol<TildeSymbol>(TildeSymbol.symbol);

    [Fact(DisplayName = "backtick")]
    public void LexBacktick() => LexSymbol<BacktickSymbol>(BacktickSymbol.symbol);

    [Fact(DisplayName = "period")]
    public void LexPeriod() => LexSymbol<PeriodSymbol>(PeriodSymbol.symbol);
}
