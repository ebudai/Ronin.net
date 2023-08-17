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

    public override bool Equals(object obj) => (obj as Token)?.Memory.Span.SequenceEqual(Memory.Span) ?? false;

    public override int GetHashCode() => Memory.Span.ToHashCode();
}