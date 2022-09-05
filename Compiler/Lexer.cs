using Ronin.Token;

namespace Ronin.Compiler;

internal class Lexer
{
    internal Lexer(string sourcecode)
    {
        Sourcecode = sourcecode.AsMemory();
    }

    internal ReadOnlyMemory<char> Sourcecode { get; }

    internal int Cursor { get; set; }
    internal int Line { get; set; } = 1;
    internal string Error { get; set; }
    internal bool IsEmpty => Span.IsEmpty;
    internal int Length => Span.Length;

    internal ReadOnlySpan<char> Span => Sourcecode[Cursor..].Span;
    internal char this[int index] => Span[index];
    internal ReadOnlyMemory<char> this[Range range] => Sourcecode[Cursor..][range];
    
    internal bool StartsWith(string text) => Span.StartsWith(text);
    internal int IndexOfAny(char[] characters) => Span.IndexOfAny(characters);

    internal List<Token.Token> Lex()
    {
        List<Token.Token> tokens = new();

        while (Cursor < Sourcecode.Length)
        {
            var token = Whitespace.Lex(this)
                ?? Literal.Lex(this)
                ?? Comment.Lex(this)
                ?? Symbol.Lex(this)
                ?? Keyword.Lex(this)
                ?? Name.Lex(this)
                ?? Token.Error.Lex(this);
            tokens.Add(token);
        }

        return tokens;
    }
}
