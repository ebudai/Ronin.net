using Ronin.Compiler;
using Ronin.Lexicon;

namespace Unit;

[Trait(nameof(Lexer), null)]
public class Symbols
{
    private static void LexSymbol(char lexed) => LexSymbol(new string(lexed, 1));

    private static void LexSymbol(string lexed)
    {
        Lexer lexer = new(lexed);
        Assert.False(lexer.IsEmpty);
        for (var i = 0; i != lexed.Length; ++i) Assert.True(char.IsSymbol(lexed[i]) || char.IsPunctuation(lexed[i]));
        var symbol = Symbol.Special.Lex(ref lexer) ?? Punctuation.Lex(ref lexer) ?? Symbol.Lex(ref lexer);

        Assert.Equal(lexed.ToArray(), symbol?.Memory.ToArray());
    }

    [Fact(DisplayName = "terminal")]
    public void LexTerminal() => LexSymbol(Terminal.symbol);

    [Fact(DisplayName = "separator")]
    public void LexSeparator() => LexSymbol(Separator.symbol);

    [Fact(DisplayName = "start scope")]
    public void LexOpenBrace() => LexSymbol(Open.Brace.symbol);

    [Fact(DisplayName = "start values")]
    public void LexOpenParenthesis() => LexSymbol(Open.Parenthesis.symbol);

    [Fact(DisplayName = "start indexer")]
    public void LexOpenSquareBracket() => LexSymbol(Open.SquareBracket.symbol);

    [Fact(DisplayName = "end scope")]
    public void LexCloseBrace() => LexSymbol(Close.Brace.symbol);

    [Fact(DisplayName = "end values")]
    public void LexCloseParenthesis() => LexSymbol(Close.Parenthesis.symbol);

    [Fact(DisplayName = "end indexer")]
    public void LexCloseSquareBracket() => LexSymbol(Close.SquareBracket.symbol);

    [Fact(DisplayName = "text delimiter")]
    public void LexDoubleQuote() => LexSymbol(TextDelimiter.symbol);

    [Fact(DisplayName = "returns")]
    public void LexReturns() => LexSymbol(Returns.symbol);

    [Fact(DisplayName = "assign")]
    public void LexAssign() => LexSymbol(Assign.symbol);

    [Fact(DisplayName = "add assign")]
    public void LexAddAssign() => LexSymbol(AddAssign.symbol);

    [Fact(DisplayName = "and assign")]
    public void LexAndAssign() => LexSymbol(AndAssign.symbol);

    [Fact(DisplayName = "and assign")]
    public void LexDivideAssign() => LexSymbol(DivideAssign.symbol);

    [Fact(DisplayName = "multiply assign")]
    public void LexMultiplyAssign() => LexSymbol(MultiplyAssign.symbol);

    [Fact(DisplayName = "or assign")]
    public void LexOrAssign() => LexSymbol(OrAssign.symbol);

    [Fact(DisplayName = "subtract assign")]
    public void LexSubtractAssign() => LexSymbol(SubtractAssign.symbol);

    [Fact(DisplayName = "elipsis")]
    public void LexElipsis() => LexSymbol(Symbol.Special.Elipsis.symbol);

    [Fact(DisplayName = "interval")]
    public void LexInterval() => LexSymbol(Symbol.Special.Interval.symbol);

    [Fact(DisplayName = "greater than or equal")]
    public void LexGreaterThanOrEqual() => LexSymbol(Symbol.Special.GreaterThanOrEqual.symbol);

    [Fact(DisplayName = "less than or equal")]
    public void LexLessThanOrEqual() => LexSymbol(Symbol.Special.LessThanOrEqual.symbol);

    [Fact(DisplayName = "not punctuation")]
    public void NotPunctuation()
    {
        const string plus = "+";

        Lexer lexer = new(plus);
        var lexed = Symbol.Lex(ref lexer);

        Assert.IsType<Symbol>(lexed);
    }

    [Fact(DisplayName = "punctuation")]
    public void IsPunctuation()
    {
        const string plus = ")";

        Lexer lexer = new(plus);
        var lexed = Punctuation.Lex(ref lexer);

        Assert.NotNull(lexed);
    }
}
