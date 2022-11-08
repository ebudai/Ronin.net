using Ronin.Compiler;

namespace Ronin.Lexicon;

public abstract class Token
{
    public override string ToString() => Sourcecode.ToString();

    protected internal ReadOnlyMemory<char> Sourcecode { get; }
    internal SourceLocation[] SourceLocations { get; init; }

    protected internal Token(Lexer lexer, int length)
    {
        Sourcecode = lexer[..length].ToArray();
        lexer.Cursor += length;
    }
}