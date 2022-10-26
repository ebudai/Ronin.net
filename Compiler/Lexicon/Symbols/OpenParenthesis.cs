using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class OpenParenthesis : Open
{
    public const char character = '(';

    public OpenParenthesis(Lexer lexer) : base(lexer, 1) { }

    public static new OpenParenthesis Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new OpenParenthesis(lexer) : null;
}
