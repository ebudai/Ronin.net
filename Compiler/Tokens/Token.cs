using Ronin.Compiler;

namespace Ronin.Tokens;

internal abstract class Token
{
    protected internal Token(Lexer lexer, int length)
    {
        Sourcecode = lexer[..length].ToArray();
        lexer.Cursor += length;
    }

    protected internal ReadOnlyMemory<char> Sourcecode { get; }
}

internal interface ILexable<T> where T : Token
{
    public static abstract T Lex(Lexer lexer);
}