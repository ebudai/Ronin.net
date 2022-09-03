using Ronin.Compiler;

namespace Ronin.Tokens.Modifiers;

internal class Variable : Token, ILexable<Variable>
{
    public Variable(Lexer lexer, int length) : base(lexer, length) { }

    public static Variable Lex(Lexer lexer)
    {
        const string keyword = "var ";

        if (lexer.Sourcecode.Length < keyword.Length) return null;

        return lexer.Sourcecode.Span.StartsWith(keyword) ? new Variable(lexer, 1) : null;
    }
}
