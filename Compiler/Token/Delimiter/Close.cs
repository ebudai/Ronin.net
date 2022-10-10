using Ronin.Compiler;

namespace Ronin.Token.Delimiter;

internal class Close : Symbol
{
    public Close(Lexer lexer, int length) : base(lexer, length) { }
}
