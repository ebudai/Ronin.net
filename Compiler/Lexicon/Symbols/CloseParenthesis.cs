using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class CloseParenthesis : Close
{
    public const char character = ')';
    public const string symbol = ")";

    public static new CloseParenthesis Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new CloseParenthesis { sourcecode = lexer.Commit(symbol.Length) };
    }
}
