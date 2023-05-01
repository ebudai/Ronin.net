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
        => Interval.Parse(ref current)
        ?? Literal.Parse(ref current)
        ?? Delegate.Parse(ref current)
        ?? InlineLookup.Parse(ref current)        
        ?? Arguments.Parse(ref current)
        ?? InlineList.Parse(ref current)
        ?? Ordinal.Parse(ref current)
        ?? Parameters.Parse(ref current) as Anonymous;
}