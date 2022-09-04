using Ronin.Compiler;

namespace Ronin.Tokens.Symbols;

internal class CloseParenthesis : Token, ILexable<CloseParenthesis>
{
    public CloseParenthesis(Lexer lexer) : base(lexer, 1) { }

    public static CloseParenthesis Lex(Lexer lexer) => lexer.IsEmpty || lexer[0] is not ')' ? null : new CloseParenthesis(lexer);
}
