using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class SubtractAssign : Assign
{
    internal new const string symbol = "-=";

    public static new SubtractAssign Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer.StartsWith(symbol) is false) return null;
        return new SubtractAssign { Memory = lexer.Commit(symbol.Length) };
    }
}
