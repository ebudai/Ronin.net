using Ronin.Compiler;

namespace Ronin.Tokens.Symbols;

internal class Separator : Token, ILexable<Separator>
{
    public Separator(Lexer lexer, int length) : base(lexer, length) { }

    public static Separator Lex(Lexer lexer)
    {
        var span = lexer.Sourcecode.Span;

        if (span.IsEmpty) return null;

        if (span[0] is not ',') return null;

        return new Separator(lexer, 1);
    }
}