using Ronin.Compiler;

namespace Ronin.Tokens.Symbols;

internal class OpenSquareBracket : Token, ILexable<OpenSquareBracket>
{
    public OpenSquareBracket(Lexer lexer, int length) : base(lexer, length) { }

    public static OpenSquareBracket Lex(Lexer lexer)
    {
        var span = lexer.Sourcecode.Span;

        if (span.IsEmpty) return null;

        if (span[0] is not '[') return null;

        return new OpenSquareBracket(lexer, 1);
    }
}
