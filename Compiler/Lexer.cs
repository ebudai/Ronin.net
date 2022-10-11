using Ronin.Token;

namespace Ronin.Compiler;

public class Lexer
{
    public Lexer(string sourcecode)
    {
        Sourcecode = sourcecode.AsMemory();
    }

    internal ReadOnlyMemory<char> Sourcecode { get; }

    internal int Cursor { get; set; }
    internal int Line { get; set; } = 1;
    internal bool IsEmpty => Span.IsEmpty;
    internal int Length => Span.Length;

    internal ReadOnlySpan<char> Span => Sourcecode[Cursor..].Span;
    internal char this[int index] => Span[index];
    internal ReadOnlyMemory<char> this[Range range] => Sourcecode[Cursor..][range];
    
    internal bool StartsWith(string text) => Span.StartsWith(text);

    public Lexeme[] Lex()
    {
        List<Lexeme> tokens = new();

        while (Cursor < Sourcecode.Length)
        {
            tokens.Add(Whitespace.Lex(this)
                ?? Literal.Lex(this) 
                ?? Comment.Lex(this)
                ?? Symbol.Lex(this)
                ?? Keyword.Lex(this)
                ?? Name.Lex(this));
        }

        return tokens.ToArray();
    }
}
