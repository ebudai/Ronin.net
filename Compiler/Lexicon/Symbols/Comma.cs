using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Comma : Punctuation
{
    public const char character = ',';
    public const string symbol = ",";

    public static new Comma Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new Comma { Sourcecode = lexer.Commit(symbol.Length) };
    }
}
