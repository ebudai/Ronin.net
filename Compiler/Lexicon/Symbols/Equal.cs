using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Equal : Punctuation
{
    public const char character = '=';
    public const string symbol = "=";

    public static new Equal Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new Equal { sourcecode = lexer.Commit(symbol.Length) };
    }
}
