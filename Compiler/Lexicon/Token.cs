// Copyright © 2023 Eric Budai

using System;
using System.Buffers;

namespace Ronin.Lexicon;

public abstract class Token : ReadOnlySequenceSegment<char>
{
    /// <summary>
    ///     Where this token begins in the text it was lexed from.
    /// </summary>
    ///
    /// <remarks>
    ///     Not <see cref="ReadOnlySequenceSegment{T}.RunningIndex"/>, which is the
    ///     offset within the sequence of tokens that were kept — and trivia is
    ///     not kept, so every skipped space and comment shifts it away from the
    ///     source. The memory is a slice of the source string and knows where it
    ///     was cut from.
    /// </remarks>
    public int Offset
    {
        get
        {
            System.Runtime.InteropServices.MemoryMarshal.TryGetString(Memory, out _, out var start, out _);
            return start;
        }
    }

    /// <remarks>
    ///     <see cref="ReadOnlySequenceSegment{T}.RunningIndex"/> is the offset of
    ///     a segment within the sequence, so it advances by the preceding
    ///     segment's length. Advancing by one made it a token counter, which left
    ///     every <c>SequencePosition</c> wrong and was the same defect as
    ///     <c>Parser.AdvanceTo</c> sizing an array from a running-index delta.
    /// </remarks>
    public Token Append(Token token)
    {
        Next = token;
        token.RunningIndex = RunningIndex + Memory.Length;
        return token;
    }

    public override bool Equals(object obj) => (obj as Token)?.Memory.Span.SequenceEqual(Memory.Span) ?? false;

    /// <remarks>Over the characters, because that is what <see cref="Equals"/> compares.</remarks>
    public override int GetHashCode()
    {
        HashCode hashCode = new();
        foreach (var character in Memory.Span)
        {
            hashCode.Add(character);
        }
        return hashCode.ToHashCode();
    }
}
