using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Returns : Punctuation
{
    public const string symbol = "=>";

    public static new Returns Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer.DoesNotStartWith(symbol)) return null;
        return new Returns { Sourcecode = lexer.Commit(symbol.Length) };
    }
}
