using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class AddAssign : Punctuation
{
    internal const string symbol = "+=";

    public static new AddAssign Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer.StartsWith(symbol) is false) return null;
        return new AddAssign { Memory = lexer.Commit(symbol.Length) };
    }
}
