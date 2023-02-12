using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class DoubleQuote : Punctuation
{
    public const char character = '"';
    public const string symbol = "\"";

    public static new DoubleQuote Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new DoubleQuote { Sourcecode = lexer.Commit(symbol.Length) };
    }
}
