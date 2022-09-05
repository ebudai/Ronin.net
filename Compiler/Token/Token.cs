using Ronin.Compiler;

namespace Ronin.Token;

internal abstract class Token
{
    protected internal Token(Lexer lexer, int length)
    {
        Sourcecode = lexer[..length].ToArray();
        lexer.Cursor += length;
    }

    protected internal ReadOnlyMemory<char> Sourcecode { get; }
}