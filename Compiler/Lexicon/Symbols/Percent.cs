using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Percent : Symbol
{
    public const char character = '%';
    public const string symbol = "%";

    public static new Percent Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new Percent { sourcecode = lexer.Commit(symbol.Length) };
    }
}
