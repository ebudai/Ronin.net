using Ronin.Compiler;

namespace Ronin.Token.Delimiter;

internal class Terminal : Symbol
{
    public const char character = ';';

    public Terminal(Lexer lexer) : base(lexer, 1) { }

    public static new Terminal Lex(Lexer lexer) => !lexer.IsEmpty && lexer[0] is character ? new Terminal(lexer) : null;
}
