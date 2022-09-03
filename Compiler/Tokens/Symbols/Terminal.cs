using Ronin.Compiler;

namespace Ronin.Tokens.Symbols;

internal class Terminal : Token, ILexable<Terminal>
{
    public Terminal(Lexer lexer, int length) : base(lexer, length) { }

    public static Terminal Lex(Lexer lexer)
    {
        var span = lexer.Sourcecode.Span;

        if (span.IsEmpty) return null;

        if (span[0] is not '.') return null;

        return new Terminal(lexer, 1);
    }
}
