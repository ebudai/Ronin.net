using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class AndAssign : Punctuation
{
    internal const string symbol = "&=";

    public static new AndAssign Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer.StartsWith(symbol) is false) return null;
        return new AndAssign { Memory = lexer.Commit(symbol.Length) };
    }
}
