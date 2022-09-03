using Ronin.Compiler;

namespace Ronin.Tokens.Modifiers;

internal class Constant : Token, ILexable<Constant>
{
    public Constant(Lexer lexer, int length) : base(lexer, length) { }

    public static Constant Lex(Lexer lexer)
    {
        const string keyword = "constant ";

        if (lexer.Sourcecode.Length < keyword.Length) return null;

        return lexer.Sourcecode.Span.StartsWith(keyword) ? new Constant(lexer, 1) : null;
    }
}
