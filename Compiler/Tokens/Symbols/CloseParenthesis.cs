using Ronin.Compiler;

namespace Ronin.Tokens.Symbols;

internal class CloseParenthesis : Token, ILexable<CloseParenthesis>
{
    public CloseParenthesis(Lexer lexer, int length) : base(lexer, length) { }

    public static CloseParenthesis Lex(Lexer lexer)
    {
        var span = lexer.Sourcecode.Span;

        if (span.IsEmpty) return null;

        if (span[0] is not ')') return null;

        return new CloseParenthesis(lexer, 1);
    }
}
