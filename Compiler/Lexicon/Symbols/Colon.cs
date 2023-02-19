using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Colon : Symbol
{
    public const char character = ':';
    public const string symbol = ":";

    public static new Colon Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new Colon { sourcecode = lexer.Commit(symbol.Length) };
    }
}