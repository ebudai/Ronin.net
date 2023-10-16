// Copyright © 2023 Eric Budai

using System;
using System.Buffers;

namespace Ronin.Lexicon;

public abstract class Token : ReadOnlySequenceSegment<char>
{
    public Token Append(Token token)
    {
        Next = token;
        token.RunningIndex = RunningIndex + 1;
        return token;
    }

    public override bool Equals(object obj) => (obj as Token)?.Memory.Span.SequenceEqual(Memory.Span) ?? false;

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