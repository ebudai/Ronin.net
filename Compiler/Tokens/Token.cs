using Ronin.Compiler;

namespace Ronin.Tokens;

internal abstract class Token
{
    protected internal Token(Lexer lexer, int length)
    {
        Sourcecode = lexer.Sourcecode[lexer.Cursor..][..length];
        SourceIndex = length;
        lexer.Cursor += length;
    }

    protected internal ReadOnlyMemory<char> Sourcecode { get; }
    protected internal int SourceIndex { get; }
}

internal interface ILexable<T> where T : Token
{
    public static abstract T Lex(Lexer lexer);
}