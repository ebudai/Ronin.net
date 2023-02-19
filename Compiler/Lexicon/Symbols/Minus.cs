using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Minus : Symbol
{
    public const char character = '-';
    public const string symbol = "-";

    public static new Minus Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new Minus { sourcecode = lexer.Commit(symbol.Length) };
    }
}
