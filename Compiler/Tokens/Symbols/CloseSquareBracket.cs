using Ronin.Compiler;

namespace Ronin.Tokens.Symbols;

internal class CloseSquareBracket : Token, ILexable<CloseSquareBracket>
{
    public CloseSquareBracket(Lexer lexer, int length) : base(lexer, length) { }

    public static CloseSquareBracket Lex(Lexer lexer)
    {
        var span = lexer.Sourcecode.Span;

        if (span.IsEmpty) return null;

        if (span[0] is not ']') return null;

        return new CloseSquareBracket(lexer, 1);
    }
}
