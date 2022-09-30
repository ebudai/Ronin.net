using Ronin.Compiler;

namespace Ronin.Token;

internal abstract class Lexeme
{
    internal int Line { get; set; }
    internal int Column { get; set; }
    internal int Length { get; set; }

    public override string ToString() => Sourcecode.ToString();

    protected internal ReadOnlyMemory<char> Sourcecode { get; }

    protected internal Lexeme(Lexer lexer, int length)
    {
        Line = lexer.Line;
        Column = GetColumn(lexer);
        Length = length;
        Sourcecode = lexer[..length].ToArray();
        lexer.Cursor += length;
    }

    private static int GetColumn(Lexer lexer)
    {
        for (int i = Math.Min(lexer.Cursor, lexer.Length - 1); i >= 0; --i)
        {
            if (lexer[i] is '\n') return lexer.Cursor - i;
        }
        return lexer.Cursor;
    }
}