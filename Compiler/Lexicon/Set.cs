using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Set : Keyword
{
    internal const string keyword = "set";

    public static new Keyword Lex(ref Lexer lexer)
    {
        if (lexer.StartsWith(keyword) is false) return null;
        if (char.IsWhiteSpace(lexer[keyword.Length]) is false) return null;
        return new Set { Memory = lexer.Commit(keyword.Length) };
    }
}
