using Ronin.Compiler;

namespace Ronin.Tokens.Modifiers;

internal class Compiled : Token, ILexable<Compiled>
{
    public Compiled(Lexer lexer, int length) : base(lexer, length) { }

    public static Compiled Lex(Lexer lexer)
    {
        const string keyword = "compiled ";

        if (lexer.Sourcecode.Length < keyword.Length) return null;

        return lexer.Sourcecode.Span.StartsWith(keyword) ? new Compiled(lexer, 1) : null;
    }
}
