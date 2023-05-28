using Ronin.Compiler;
using Ronin.Lexicon.Punctuation;

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
    public void LexTerminal() => LexSymbol<Terminal>(Terminal.symbol);

    [Fact(DisplayName = "separator")]
    public void LexSeparator() => LexSymbol<Separator>(Separator.symbol);

    [Fact(DisplayName = "open brace")]
    public void LexOpenBrace() => LexSymbol<OpenBrace>(OpenBrace.symbol);

    [Fact(DisplayName = "open parenthesis")]
    public void LexOpenParenthesis() => LexSymbol<OpenParenthesis>(OpenParenthesis.symbol);

    [Fact(DisplayName = "open square bracket")]
    public void LexOpenSquareBracket() => LexSymbol<OpenSquareBracket>(OpenSquareBracket.symbol);

    [Fact(DisplayName = "close brace")]
    public void LexCloseBrace() => LexSymbol<CloseBrace>(CloseBrace.symbol);

    [Fact(DisplayName = "close parenthesis")]
    public void LexCloseParenthesis() => LexSymbol<CloseParenthesis>(CloseParenthesis.symbol);

    [Fact(DisplayName = "close square bracket")]
    public void LexCloseSquareBracket() => LexSymbol<CloseSquareBracket>(CloseSquareBracket.symbol);

    [Fact(DisplayName = "character delimiter")]
    public void LexSingleQuote() => LexSymbol<CharacterDelimiter>(CharacterDelimiter.symbol);

    [Fact(DisplayName = "text delimiter")]
    public void LexDoubleQuote() => LexSymbol<TextDelimiter>(TextDelimiter.symbol);

    [Fact(DisplayName = "returns")]
    public void LexReturns() => LexSymbol<Returns>(Returns.symbol);

    [Fact(DisplayName = "assign")]
    public void LexAssign() => LexSymbol<Assign>(Assign.symbol);

    [Fact(DisplayName = "ampersand")]
    public void LexAmpersand() => LexSymbol<Ampersand>(Ampersand.symbol);

    [Fact(DisplayName = "asterisk")]
    public void LexAsterisk() => LexSymbol<Asterisk>(Asterisk.symbol);

    [Fact(DisplayName = "at")]
    public void LexAt() => LexSymbol<At>(At.symbol);

    [Fact(DisplayName = "backslash")]
    public void LexBackslash() => LexSymbol<Backslash>(Backslash.symbol);

    [Fact(DisplayName = "chevron")]
    public void LexChevron() => LexSymbol<Chevron>(Chevron.symbol);

    [Fact(DisplayName = "colon")]
    public void LexColon() => LexSymbol<Colon>(Colon.symbol);

    [Fact(DisplayName = "dollar")]
    public void LexDollar() => LexSymbol<Dollar>(Dollar.symbol);

    [Fact(DisplayName = "exclamation")]
    public void LexExclamation() => LexSymbol<Exclamation>(Exclamation.symbol);

    [Fact(DisplayName = "greater than")]
    public void LexGreaterThan() => LexSymbol<GreaterThan>(GreaterThan.symbol);

    [Fact(DisplayName = "less than")]
    public void LexLessThan() => LexSymbol<LessThan>(LessThan.symbol);

    [Fact(DisplayName = "minus")]
    public void LexMinus() => LexSymbol<Minus>(Minus.symbol);

    [Fact(DisplayName = "percent")]
    public void LexPercent() => LexSymbol<Percent>(Percent.symbol);

    [Fact(DisplayName = "pipe")]
    public void LexPipe() => LexSymbol<Pipe>(Pipe.symbol);

    [Fact(DisplayName = "plus")]
    public void LexPlus() => LexSymbol<Plus>(Plus.symbol);

    [Fact(DisplayName = "pound")]
    public void LexPound() => LexSymbol<Pound>(Pound.symbol);

    [Fact(DisplayName = "question")]
    public void LexQuestion() => LexSymbol<Question>(Question.symbol);

    [Fact(DisplayName = "slash")]
    public void LexSlash() => LexSymbol<Slash>(Slash.symbol);

    [Fact(DisplayName = "tilde")]
    public void LexTilde() => LexSymbol<Tilde>(Tilde.symbol);

    [Fact(DisplayName = "backtick")]
    public void LexBacktick() => LexSymbol<Backtick>(Backtick.symbol);

    [Fact(DisplayName = "period")]
    public void LexPeriod() => LexSymbol<Period>(Period.symbol);
}
