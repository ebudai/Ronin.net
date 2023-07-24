using Ronin.Compiler;
using Ronin.Lexicon;

namespace Unit;

[Trait("Lexer", null)]
public class Symbols
{
    private static void LexSymbol<T>(char lexed) where T : Symbol => LexSymbol<T>(new string(lexed, 1));

    private static void LexSymbol<T>(string lexed) where T : Symbol
    {
        Lexer lexer = new(lexed);
        Assert.False(lexer.IsEmpty);
        for (var i = 0; i != lexed.Length; ++i) Assert.True(char.IsSymbol(lexed[i]) || char.IsPunctuation(lexed[i]));
        var symbol = Symbol.Lex(ref lexer) as T;

        Assert.Equal(lexed.ToArray(), symbol?.Memory.ToArray());
    }

    [Fact(DisplayName = "terminal")]
    public void LexTerminal() => LexSymbol<Terminal>(Terminal.symbol);

    [Fact(DisplayName = "separator")]
    public void LexSeparator() => LexSymbol<Separator>(Separator.symbol);

    [Fact(DisplayName = "start scope")]
    public void LexOpenBrace() => LexSymbol<StartScope>(StartScope.symbol);

    [Fact(DisplayName = "start values")]
    public void LexOpenParenthesis() => LexSymbol<StartValues>(StartValues.symbol);

    [Fact(DisplayName = "start indexer")]
    public void LexOpenSquareBracket() => LexSymbol<StartIndexer>(StartIndexer.symbol);

    [Fact(DisplayName = "end scope")]
    public void LexCloseBrace() => LexSymbol<EndScope>(EndScope.symbol);

    [Fact(DisplayName = "end values")]
    public void LexCloseParenthesis() => LexSymbol<EndValues>(EndValues.symbol);

    [Fact(DisplayName = "end indexer")]
    public void LexCloseSquareBracket() => LexSymbol<EndIndexer>(EndIndexer.symbol);

    [Fact(DisplayName = "character delimiter")]
    public void LexSingleQuote() => LexSymbol<CharacterDelimiter>(CharacterDelimiter.symbol);

    [Fact(DisplayName = "text delimiter")]
    public void LexDoubleQuote() => LexSymbol<TextDelimiter>(TextDelimiter.symbol);

    [Fact(DisplayName = "returns")]
    public void LexReturns() => LexSymbol<Returns>(Returns.symbol);

    [Fact(DisplayName = "assign")]
    public void LexAssign() => LexSymbol<Assign>(Assign.symbol);

    [Fact(DisplayName = "add assign")]
    public void LexAddAssign() => LexSymbol<AddAssign>(AddAssign.symbol);

    [Fact(DisplayName = "and assign")]
    public void LexAndAssign() => LexSymbol<AndAssign>(AndAssign.symbol);

    [Fact(DisplayName = "and assign")]
    public void LexDivideAssign() => LexSymbol<DivideAssign>(DivideAssign.symbol);

    [Fact(DisplayName = "multiply assign")]
    public void LexMultiplyAssign() => LexSymbol<MultiplyAssign>(MultiplyAssign.symbol);

    [Fact(DisplayName = "or assign")]
    public void LexOrAssign() => LexSymbol<OrAssign>(OrAssign.symbol);

    [Fact(DisplayName = "subtract assign")]
    public void LexSubtractAssign() => LexSymbol<SubtractAssign>(SubtractAssign.symbol);

    [Fact(DisplayName = "not punctuation")]
    public void NotPunctuation()
    {
        const string plus = "+";

        Lexer lexer = new(plus);
        var lexed = Symbol.Lex(ref lexer);

        Assert.IsType<Symbol>(lexed);
    }

    [Fact(DisplayName = "punctuation")]
    public void Punctuation()
    {
        const string plus = ")";

        Lexer lexer = new(plus);
        var lexed = Symbol.Lex(ref lexer);

        Assert.IsAssignableFrom<Punctuation>(lexed);
    }
}
