using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class OrAssign : Assign
{
    internal new const string symbol = "|=";

    public static new OrAssign Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer.StartsWith(symbol) is false) return null;
        return new OrAssign { Memory = lexer.Commit(symbol.Length) };
    }
}
