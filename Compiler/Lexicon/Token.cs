// Copyright © 2023 Eric Budai

using System;
using System.Buffers;

namespace Ronin.Lexicon;

public abstract class Token : ReadOnlySequenceSegment<char>
{
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