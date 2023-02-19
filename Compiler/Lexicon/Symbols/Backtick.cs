using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Backtick : Symbol
{
    public const char character = '`';
    public const string symbol = "`";

    public static new Backtick Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new Backtick { sourcecode = lexer.Commit(symbol.Length) };
    }
}
