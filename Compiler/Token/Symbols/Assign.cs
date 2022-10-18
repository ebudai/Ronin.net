using Ronin.Compiler;

namespace Ronin.Token.Symbols;

internal class Assign : Symbol
{
    public const char character = '=';

    public Assign(Lexer lexer) : base(lexer, 1) { }

    public static new Assign Lex(Lexer lexer) => !lexer.IsEmpty && lexer[0] is character ? new Assign(lexer) : null;
}
