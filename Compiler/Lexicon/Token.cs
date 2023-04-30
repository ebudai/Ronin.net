// Copyright © 2023 Eric Budai

namespace Ronin.Lexicon;

public abstract class Token
{
    public override string ToString() => sourcecode.ToString();

    protected internal ReadOnlyMemory<char> sourcecode;
}