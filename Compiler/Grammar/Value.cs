using Ronin.Compiler;

namespace Ronin.Grammar;

/// <summary>
///     Base class representing any <see cref="Anonymous"/> or <see cref="Reference"/>d value
/// </summary>
internal class Value : Statement, IParsableSyntax<Value>
{
    public new static Value Parse(ref Parser current) 
        => Context.Member.Parse(ref current)
        ?? Anonymous.Parse(ref current) as Value;

    /// <summary>
    ///     Represents a <see cref="Value"/> before it has been assigned or bound to a parameter
    /// </summary>
    public class Anonymous : Value, IParsableSyntax<Anonymous>
    {
        public new static Anonymous Parse(ref Parser current)
            => Inline.Parse(ref current)
            ?? Delegate.Declaration.Parse(ref current)
            ?? Lookup.Parse(ref current)
            ?? Inputs.Parse(ref current)
            ?? List.Parse(ref current)
            ?? Indexer.Parse(ref current) as Anonymous;
    }
}