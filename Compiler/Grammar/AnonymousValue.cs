// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Compound;

namespace Ronin.Grammar;

/// <summary>
///     Represents a <see cref="Value"/> before it has been assigned or bound to a parameter
/// </summary>
internal class AnonymousValue : Value, IParsableSyntax<AnonymousValue>
{
    public new static AnonymousValue Parse(ref Parser current)
        => Inline.Parse(ref current)
        ?? Delegate.Parse(ref current)
        ?? Lookup.Parse(ref current)
        ?? Inputs.Parse(ref current)
        ?? List.Parse(ref current)
        ?? Indexer.Parse(ref current) as AnonymousValue;
}