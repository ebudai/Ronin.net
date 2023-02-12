using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Backslash : Symbol
{
    public const char character = '\\';
    public const string symbol = "\\";

    public static new Backslash Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new Backslash { Sourcecode = lexer.Commit(symbol.Length) };
    }
}
