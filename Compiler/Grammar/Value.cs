using Ronin.Compiler;

namespace Ronin.Grammar;

/// <summary>
///     Base class representing any <see cref="Anonymous"/> or <see cref="Reference"/>d value
/// </summary>
internal class Value : Statement, IParsableSyntax<Value>
{
    public new static Value Parse(ref Parser current) 
        => Anonymous.Parse(ref current) 
        ?? Reference.Parse(ref current) as Value;
}