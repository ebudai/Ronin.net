using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class OpenParenthesis : Open
{
    public const char character = '(';
    public const string symbol = "(";

    private OpenParenthesis(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new OpenParenthesis Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new OpenParenthesis(lexer) : null;
}
