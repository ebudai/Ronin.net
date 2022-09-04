using Ronin.Compiler;

namespace Ronin.Tokens.Symbols;

internal class CloseBrace : Token, ILexable<CloseBrace>
{
    public CloseBrace(Lexer lexer) : base(lexer, 1) { }

    public static CloseBrace Lex(Lexer lexer) => lexer.IsEmpty || lexer[0] is not '}' ? null : new CloseBrace(lexer);
}
