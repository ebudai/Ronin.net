// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Compound;

namespace Ronin.Grammar;

/// <summary>
///     Represents a <see cref="Value"/> before it has been assigned or bound to a parameter
/// </summary>
internal class Anonymous : Value, IParsableSyntax<Anonymous>
{
    public new static Anonymous Parse(ref Parser current)
        => Literal.Parse(ref current)
        ?? Delegate.Parse(ref current)
        ?? Lookup.Parse(ref current)        
        ?? Inputs.Parse(ref current)
        ?? List.Parse(ref current)
        ?? Ordinal.Parse(ref current) as Anonymous;
}