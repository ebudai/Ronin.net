using Ronin.Lexicon;

namespace Ronin.Compiler;

internal class Lexer
{
    public Lexer(string sourcecode)
    {
        Sourcecode = sourcecode.AsMemory();
    }

    public Token[] Lex()
    {
        List<Token> tokens = new(64);

        while (Cursor < Sourcecode.Length)
        {
            var token = Whitespace.Lex(this)
                ?? Literal.Lex(this)
                ?? Comment.Lex(this)
                ?? Symbol.Lex(this)
                ?? Keyword.Lex(this)
                ?? Word.Lex(this) as Token;
            tokens.Add(token);
            Column += token.SourceLocation.Length;
        }

        tokens.Add(Sentinel.Instance);

        return tokens.ToArray();
    }

    internal ReadOnlyMemory<char> Sourcecode { get; }

    internal int Cursor { get; set; }
    internal int Line
    {
        get => line;
        set
        {
            line = value;
            Column = 0;
        }
    }
    internal int Column { get; set; }
    internal bool IsEmpty => Span.IsEmpty;
    internal bool IsNotEmpty => IsEmpty is false;
    internal int Length => Span.Length;

    internal ReadOnlySpan<char> Span => Sourcecode[Cursor..].Span;
    internal ref readonly char this[int index] => ref Span[index];
    internal ReadOnlyMemory<char> this[Range range] => Sourcecode[Cursor..][range];
    
    internal bool StartsWith(string text) => Span.StartsWith(text);
    internal bool DoesNotStartWith(string text) => StartsWith(text) is not true;

    private int line = 1;
}
