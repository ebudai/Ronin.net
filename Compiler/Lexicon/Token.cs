// Copyright © 2023 Eric Budai

using System.Buffers;

namespace Ronin.Lexicon;

public abstract class Token : ReadOnlySequenceSegment<char>
{
    public void Append(in Token token)
    {
        Next = token;
        ++RunningIndex;
    }
}
