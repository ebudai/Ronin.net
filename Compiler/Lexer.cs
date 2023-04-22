// Copyright © 2023 Eric Budai

using Ronin.Lexicon;

namespace Ronin.Compiler;

internal struct Lexer
{
    public Lexer(in string sourcecode) => this.sourcecode = sourcecode.AsMemory();

    public IEnumerable<Token> Lex()
    {
        while (cursor < sourcecode.Length)
        {
            var token = Whitespace.Lex(ref this)
                ?? Literal.Lex(ref this)
                ?? Comment.Lex(ref this)
                ?? Symbol.Lex(ref this)
                ?? Reserved.Lex(ref this) as Token
                ?? Word.Lex(ref this);
            yield return token;
        }

        yield return Sentinel.Instance;
    }

    public ReadOnlyMemory<char> Commit(int length)
    {
        var memory = sourcecode.Slice(cursor, length);
        cursor += length;
        return memory;
    }

    public bool IsEmpty => sourcecode[cursor..].IsEmpty;
    public bool IsNotEmpty => IsEmpty is false;
    public int Length => sourcecode[cursor..].Length;

    public readonly ref readonly char this[int index] => ref sourcecode.Span[cursor..][index];
    public readonly ReadOnlyMemory<char> this[Range range] => sourcecode[cursor..][range];

    public bool StartsWith(string text) => sourcecode[cursor..].Span.StartsWith(text);
    public bool DoesNotStartWith(string text) => StartsWith(text) is not true;

    public int IndexOf(char character) => sourcecode.Span[cursor..].IndexOf(character);

    private int cursor;
    private readonly ReadOnlyMemory<char> sourcecode;
}
