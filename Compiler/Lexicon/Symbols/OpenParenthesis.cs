using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class OpenParenthesis : Open
{
    public const char character = '(';
    public const string symbol = "(";

    public static new OpenParenthesis Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new OpenParenthesis { Sourcecode = lexer.Commit(symbol.Length) };
    }
}
