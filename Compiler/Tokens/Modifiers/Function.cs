using Ronin.Compiler;

namespace Ronin.Tokens.Modifiers;

internal class Function : Token, ILexable<Function>
{
    public Function(Lexer lexer, int length) : base(lexer, length) { }

    public static Function Lex(Lexer lexer)
    {
        const string keyword = "function ";

        if (lexer.Sourcecode.Length < keyword.Length) return null;

        return lexer.Sourcecode.Span.StartsWith(keyword) ? new Function(lexer, 1) : null;
    }
}
