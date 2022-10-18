using Ronin.Compiler;

namespace Ronin.Token.Symbols;

internal class Open : Symbol
{
    public Open(Lexer lexer, int length) : base(lexer, length) { }
}
