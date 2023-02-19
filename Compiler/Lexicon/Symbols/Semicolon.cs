using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Semicolon : Punctuation
{
    public const char character = ';';
    public const string symbol = ";";

    public static new Semicolon Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new Semicolon { sourcecode = lexer.Commit(symbol.Length) };
    }
}
