using Ronin.Compiler;

namespace Ronin.Token.Symbols;

internal class CloseBrace : Close
{
    public const char character = '}';

    public CloseBrace(Lexer lexer) : base(lexer, 1) { }

    public static new CloseBrace Lex(Lexer lexer) => !lexer.IsEmpty && lexer[0] is character ? new CloseBrace(lexer) : null;
}
