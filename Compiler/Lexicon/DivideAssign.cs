using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class DivideAssign : Punctuation
{
    internal const string symbol = "/=";

    public static new DivideAssign Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer.StartsWith(symbol) is false) return null;
        return new DivideAssign { Memory = lexer.Commit(symbol.Length) };
    }
}
