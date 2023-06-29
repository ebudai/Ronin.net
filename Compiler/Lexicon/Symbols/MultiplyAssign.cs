using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class MultiplyAssign : Punctuation
{
    internal const string symbol = "*=";

    public static new MultiplyAssign Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer.StartsWith(symbol) is false) return null;
        return new MultiplyAssign { Memory = lexer.Commit(symbol.Length) };
    }
}
