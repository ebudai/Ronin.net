using Ronin.Compiler;

namespace Ronin.Tokens.Symbols;

internal class OpenSquareBracket : Token, ILexable<OpenSquareBracket>
{
    public OpenSquareBracket(Lexer lexer) : base(lexer, 1) { }

    public static OpenSquareBracket Lex(Lexer lexer) => lexer.IsEmpty || lexer[0] is not '[' ? null : new OpenSquareBracket(lexer);
}
