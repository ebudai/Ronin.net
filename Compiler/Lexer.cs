// Copyright © 2023 Eric Budai

using Ronin.Lexicon;
using System;

namespace Ronin.Compiler;

internal ref struct Lexer
{
    public Lexer(in string sourcecode) => this.sourcecode = ref sourcecode;

    public Token Lex()
    {
        Token start = null;
        Token current = null;
        while (cursor < sourcecode.Length)
        {
            var token = Literal.Lex(ref this)                
                ?? Special.Lex(ref this)
                ?? Punctuation.Lex(ref this)
                ?? Keyword.Lex(ref this)
                ?? Trivium.Lex(ref this)
                ?? Symbol.Lex(ref this)
                ?? Word.Lex(ref this) as Token;
            if (token is Trivium) continue;
            current = current?.Append(token) ?? token;
            start ??= current;
        }

        current?.Append(new Sentinel());
        return start;
    }

    public ReadOnlyMemory<char> AdvanceBy(int length)
    {
        var memory = sourcecode.AsMemory().Slice(cursor, length);
        cursor += length;
        return memory;
    }

    public readonly bool IsEmpty => cursor >= sourcecode.Length;
    public readonly int Length => sourcecode.Length - cursor;

    public readonly char this[int index] => sourcecode[cursor + index];
    public readonly ReadOnlySpan<char> this[in Range range] => sourcecode.AsSpan()[cursor..][range];

    public readonly bool StartsWith(string text) => sourcecode.IndexOf(text, cursor) == cursor;

    public readonly int IndexOf(char character) => sourcecode.IndexOf(character, cursor);

    private int cursor;
    private readonly ref readonly string sourcecode;
}
