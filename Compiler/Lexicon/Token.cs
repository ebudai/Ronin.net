using Ronin.Compiler;

namespace Ronin.Lexicon;

public record struct SourceLocation(int Line, int Column, int Length);

public abstract class Token
{
    public override string ToString() => Sourcecode.ToString();

    public static implicit operator string(Token token) => token.ToString();

    protected internal ReadOnlyMemory<char> Sourcecode { get; }
    internal SourceLocation SourceLocation { get; }

    protected internal Token(Lexer lexer, int length)
    {
        Sourcecode = lexer[..length].ToArray();
        SourceLocation = new(lexer.Line, lexer.Column, length);
        lexer.Cursor += length;
    }
}