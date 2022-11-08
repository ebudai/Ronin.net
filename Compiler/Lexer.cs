using Ronin.Lexicon;

namespace Ronin.Compiler;

public class Lexer
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
            Token token = Whitespace.Lex(this)
                ?? Literal.Lex(this)
                ?? Comment.Lex(this)
                ?? Symbol.Lex(this)
                ?? Keyword.Lex(this)
                ?? Word.Lex(this) as Token;
            /*if (token is not Trivium) */tokens.Add(token);
        }

        tokens.Add(Sentinel.Instance);

        return tokens.ToArray();
    }

    internal ReadOnlyMemory<char> Sourcecode { get; }

    internal int Cursor { get; set; }
    internal int Line { get; set; } = 1;
    internal bool IsEmpty => Span.IsEmpty;
    internal bool IsNotEmpty => IsEmpty is false;
    internal int Length => Span.Length;

    internal ReadOnlySpan<char> Span => Sourcecode[Cursor..].Span;
    internal ref readonly char this[int index] => ref Span[index];
    internal ReadOnlyMemory<char> this[Range range] => Sourcecode[Cursor..][range];
    
    internal bool StartsWith(string text) => Span.StartsWith(text);
    internal bool DoesNotStartWith(string text) => StartsWith(text) is not true;
}
