using Ronin.Compiler;

namespace Ronin.Grammar;

/// <summary>
///     Base class representing any <see cref="Temporary"/> or <see cref="Reference"/>d value
/// </summary>
internal class Value : Statement, IParsable<Value>
{
    /// <remarks>
    ///     A reference first, and it is the longer parse: «x =&gt; { … } [0]» is
    ///     a delegate and its indexer, where «x =&gt; { … }» alone is a delegate.
    ///     Whichever of the two is tried first has to be able to see the other,
    ///     which is why the delegate is recognised inside
    ///     <c>Reference.Component</c> rather than beside it here.
    ///     <para>
    ///     Trying <c>Delegate.Parse</c> here instead moved the premature
    ///     commitment to the other side of the same boundary — it committed as
    ///     soon as the delegate was complete, without asking whether the delegate
    ///     was the start of something longer — and speculated the production
    ///     twice, since <c>Temporary</c> holds the same branch.
    ///     </para>
    /// </remarks>
    public static new Value Parse(ref Parser current)
        => Member.Unresolved.Parse(ref current) ?? Temporary.Parse(ref current) as Value;
}

/// <summary>
///     Represents a <see cref="Value"/> before it has been assigned or bound to a parameter
/// </summary>
internal class Temporary : Value
{
    public new static Temporary Parse(ref Parser current)
        => Literal.Parse(ref current)
        ?? Delegate.Parse(ref current)
        ?? Lookup.Parse(ref current)
        ?? Inputs.Parse(ref current)
        ?? List.Parse(ref current)
        ?? Index.Parse(ref current) as Temporary;
}
