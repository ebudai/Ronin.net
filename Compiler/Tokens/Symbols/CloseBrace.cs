using Ronin.Compiler;

namespace Ronin.Tokens.Symbols;

internal class CloseBrace : Token, ILexable<CloseBrace>
{
    public CloseBrace(Lexer lexer, int length) : base(lexer, length) { }

    public static CloseBrace Lex(Lexer lexer)
    {
        var span = lexer.Sourcecode.Span;

        if (span.IsEmpty) return null;

        if (span[0] is not '}') return null;

        return new CloseBrace(lexer, 1);
    }
}
