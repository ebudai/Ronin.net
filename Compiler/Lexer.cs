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
                ?? Symbol.Special.Lex(ref this)
                ?? Punctuation.Lex(ref this)
                ?? Keyword.Lex(ref this)
                ?? Trivium.Lex(ref this)
                ?? Symbol.Lex(ref this)
                ?? Word.Lex(ref this) as Token;
            if (token is Trivium) continue;
            current = current?.Append(token) ?? token;
            start ??= current;
        }

        // An empty source is a token list of one sentinel, not a null. Returning
        // null made Parser.IsNotFinished true forever and the first Advance
        // dereferenced it.
        Sentinel sentinel = new();
        current?.Append(sentinel);
        return start ?? sentinel;
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

    /// <remarks>Ordinal: a source file is not prose, and CA1310 is right about it.</remarks>
    public readonly bool StartsWith(string text)
        => sourcecode.AsSpan(cursor).StartsWith(text, System.StringComparison.Ordinal);

    /// <summary>
    ///     Relative to the cursor, like every other member here.
    /// </summary>
    ///
    /// <remarks>
    ///     It used to return an absolute index, which <c>Comment.Lex</c> then
    ///     passed to <c>AdvanceBy</c> as a length — so a comment anywhere but the
    ///     start of a file swallowed everything after it.
    /// </remarks>
    public readonly int IndexOf(char character)
    {
        var found = sourcecode.IndexOf(character, cursor);
        return found < 0 ? found : found - cursor;
    }

    private int cursor;
    private readonly ref readonly string sourcecode;
}
