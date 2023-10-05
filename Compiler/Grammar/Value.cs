using Ronin.Compiler;

namespace Ronin.Grammar;

/// <summary>
///     Base class representing any <see cref="Temporary"/> or <see cref="Reference"/>d value
/// </summary>
internal class Value : IGrammar<Value>
{
    public static Value Parse(ref Parser current) 
        => Type.Parse(ref current)
        ?? Function.Parse(ref current)
        ?? Datum.Parse(ref current)
        ?? Temporary.Parse(ref current) as Value;

    /// <summary>
    ///     Represents a <see cref="Value"/> before it has been assigned or bound to a parameter
    /// </summary>
    public class Temporary : Value, IGrammar<Temporary>
    {
        public new static Temporary Parse(ref Parser current)
            => Literal.Parse(ref current)
            ?? Delegate.Parse(ref current)
            ?? Lookup.Parse(ref current)
            ?? Inputs.Parse(ref current)
            ?? List.Parse(ref current)
            ?? Index.Parse(ref current) as Temporary;
    }
}