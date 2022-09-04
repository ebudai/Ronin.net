using Ronin.Compiler;

namespace Ronin.Tokens.Symbols;

internal class OpenBrace : Token, ILexable<OpenBrace>
{
    public OpenBrace(Lexer lexer) : base(lexer, 1) { }

    public static OpenBrace Lex(Lexer lexer) => lexer.IsEmpty || lexer[0] is not '{' ? null : new OpenBrace(lexer);
}
