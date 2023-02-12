using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class At : Symbol
{
    public const char character = '@';
    public const string symbol = "@";

    public static new At Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new At { Sourcecode = lexer.Commit(symbol.Length) };
    }
}
