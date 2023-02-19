using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Slash : Symbol
{
    public const char character = '/';
    public const string symbol = "/";

    public static new Slash Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new Slash { sourcecode = lexer.Commit(symbol.Length) };
    }
}
