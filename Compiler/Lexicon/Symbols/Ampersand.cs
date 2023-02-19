using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Ampersand : Symbol
{
    public const char character = '&';
    public const string symbol = "&";

    public static new Ampersand Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new Ampersand { sourcecode = lexer.Commit(symbol.Length) };
    }
}
