// Copyright © 2023 Eric Budai

using System.Buffers;

namespace Ronin.Lexicon;

public abstract class Token : ReadOnlySequenceSegment<char>
{
    public void Append(Token token)
    {
        Next = token;
        token.RunningIndex = RunningIndex + 1;
    }

    public override bool Equals(object obj) => obj is Token token && Memory.Span.SequenceEqual(token.Memory.Span);

    public override int GetHashCode()
    {
        HashCode hashcode = new();
        foreach (var character in Memory.Span) hashcode.Add(character);
        return hashcode.ToHashCode();
    }
}