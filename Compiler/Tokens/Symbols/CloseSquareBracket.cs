using Ronin.Compiler;

namespace Ronin.Tokens.Symbols;

internal class CloseSquareBracket : Token, ILexable<CloseSquareBracket>
{
    public CloseSquareBracket(Lexer lexer) : base(lexer, 1) { }

    public static CloseSquareBracket Lex(Lexer lexer) => lexer.IsEmpty || lexer[0] is not ']' ? null : new CloseSquareBracket(lexer);
}
