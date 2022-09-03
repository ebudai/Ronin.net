using Ronin.Compiler;

namespace Ronin.Tokens.Symbols;

internal class OpenBrace : Token, ILexable<OpenBrace>
{
    public OpenBrace(Lexer lexer, int length) : base(lexer, length) { }

    public static OpenBrace Lex(Lexer lexer)
    {
        var span = lexer.Sourcecode.Span;

        if (span.IsEmpty) return null;

        if (span[0] is not '{') return null;

        return new OpenBrace(lexer, 1);
    }
}
