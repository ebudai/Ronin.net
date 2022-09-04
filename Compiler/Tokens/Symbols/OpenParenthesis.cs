using Ronin.Compiler;

namespace Ronin.Tokens.Symbols;

internal class OpenParenthesis : Token, ILexable<OpenParenthesis>
{
    public OpenParenthesis(Lexer lexer) : base(lexer, 1) { }

    public static OpenParenthesis Lex(Lexer lexer) => lexer.IsEmpty || lexer[0] is not '(' ? null : new OpenParenthesis(lexer);
}
