using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Question : Symbol
{
    public const char character = '?';
    public const string symbol = "?";

    public static new Question Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new Question { Sourcecode = lexer.Commit(symbol.Length) };
    }
}
