using Ronin.Compiler;

namespace Ronin.Token.Symbols;

internal class CloseSquareBracket : Close
{
    public const char character = ']';

    public CloseSquareBracket(Lexer lexer) : base(lexer, 1) { }

    public static new CloseSquareBracket Lex(Lexer lexer) => !lexer.IsEmpty && lexer[0] is character ? new CloseSquareBracket(lexer) : null;
}
