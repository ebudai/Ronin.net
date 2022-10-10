using Ronin.Compiler;

namespace Ronin.Token.Delimiter;

internal class Open : Symbol
{
    public Open(Lexer lexer, int length) : base(lexer, length) { }
}
