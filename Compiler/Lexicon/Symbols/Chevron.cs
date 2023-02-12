using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Chevron : Symbol
{
    public const char character = '^';
    public const string symbol = "^";

    public static new Chevron Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new Chevron { Sourcecode = lexer.Commit(symbol.Length) };
    }
}
