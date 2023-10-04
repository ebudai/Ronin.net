using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Punctuation : Symbol
{
    public static new Punctuation Lex(ref Lexer lexer)
        => Returns.Lex(ref lexer)
        ?? Assignment.Lex(ref lexer)
        ?? Bracket.Lex(ref lexer)
        ?? Separator.Lex(ref lexer)
        ?? Terminal.Lex(ref lexer)
        ?? TextDelimiter.Lex(ref lexer) as Punctuation;

    protected static T Lex<T>(ref Lexer lexer, string symbol) where T : Punctuation, new()
    {
        if (lexer.IsEmpty || lexer.StartsWith(symbol) is false) return null;
        return new() { Memory = lexer.Commit(symbol.Length) };
    }

    protected static T Lex<T>(ref Lexer lexer, char symbol) where T : Punctuation, new()
    {
        if (lexer.IsEmpty || lexer[0] != symbol) return null;
        return new() { Memory = lexer.Commit(1) };
    }
}

internal class Returns : Punctuation
{
    internal const string symbol = "=>";

    public static new Returns Lex(ref Lexer lexer) => Lex<Returns>(ref lexer, symbol);
}

internal class Separator : Punctuation
{
    internal const char symbol = ',';

    public static new Separator Lex(ref Lexer lexer) => Lex<Separator>(ref lexer, symbol);
}

internal class Terminal : Punctuation
{
    internal const char symbol = ';';

    public static new Terminal Lex(ref Lexer lexer) => Lex<Terminal>(ref lexer, symbol);
}

internal class TextDelimiter : Punctuation
{
    internal const char symbol = '"';

    public static new TextDelimiter Lex(ref Lexer lexer) => Lex<TextDelimiter>(ref lexer, symbol);
}
