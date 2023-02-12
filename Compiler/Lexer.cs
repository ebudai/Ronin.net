using Ronin.Lexicon;

namespace Ronin.Compiler;

//todo make this a ref struct, and instantiate tokens the same way syntax is
internal ref struct Lexer
{
    public Lexer(string sourcecode)
    {
        Sourcecode = sourcecode.AsSpan();
    }

    public List<Token> Lex()
    {
        List<Token> tokens = new();

        while (Cursor < Sourcecode.Length)
        {
            var token = Whitespace.Lex(ref this)
                ?? Literal.Lex(ref this)
                ?? Comment.Lex(ref this)
                ?? Symbol.Lex(ref this)
                ?? Keyword.Lex(ref this)
                ?? Word.Lex(ref this) as Token;
            tokens.Add(token);
        }

        tokens.Add(Sentinel.Instance);

        return tokens;
    }

    public ReadOnlyMemory<char> Commit(int length)
    {
        var memory = this[..length].ToArray();
        Cursor += length;
        return memory;
    }

    internal readonly ReadOnlySpan<char> Sourcecode { get; }

    internal int Cursor { get; set; }
    internal bool IsEmpty => Sourcecode[Cursor..].IsEmpty;
    internal int Length => Sourcecode[Cursor..].Length;

    internal ref readonly char this[int index] => ref Sourcecode[Cursor..][index];
    internal readonly ReadOnlySpan<char> this[Range range] => Sourcecode[Cursor..][range];
    
    internal bool StartsWith(string text) => Sourcecode[Cursor..].StartsWith(text);
    internal bool DoesNotStartWith(string text) => StartsWith(text) is not true;

    internal int IndexOf(char character) => Sourcecode[Cursor..].IndexOf(character);
}
