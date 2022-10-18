using Ronin.Compiler;

namespace Ronin.Token.Symbols;

internal class CloseParenthesis : Close
{
    public const char character = ')';

    public CloseParenthesis(Lexer lexer) : base(lexer, 1) { }

    public static new CloseParenthesis Lex(Lexer lexer) => !lexer.IsEmpty && lexer[0] is character ? new CloseParenthesis(lexer) : null;
}
