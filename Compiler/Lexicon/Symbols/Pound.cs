using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Pound : Symbol
{
    public const char character = '#';
    public const string symbol = "#";

    public static new Pound Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new Pound { sourcecode = lexer.Commit(symbol.Length) };
    }
}
