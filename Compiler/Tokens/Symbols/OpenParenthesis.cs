using Ronin.Compiler;

namespace Ronin.Tokens.Symbols;

internal class OpenParenthesis : Token, ILexable<OpenParenthesis>
{
    public OpenParenthesis(Lexer lexer, int length) : base(lexer, length) { }

    public static OpenParenthesis Lex(Lexer lexer)
    {
        var span = lexer.Sourcecode.Span;

        if (span.IsEmpty) return null;

        if (span[0] is not '(') return null;

        return new OpenParenthesis(lexer, 1);
    }
}
