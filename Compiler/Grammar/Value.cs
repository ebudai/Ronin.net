using Ronin.Compiler;

namespace Ronin.Grammar;

/// <summary>
///     Base class representing any <see cref="Temporary"/> or <see cref="Reference"/>d value
/// </summary>
internal class Value : Statement, IParsable<Value>
{
    /// <remarks>
    ///     A delegate FIRST, because it is the only alternative here that can be
    ///     mistaken for the start of another. «x =&gt; { … }» is the documented
    ///     bare form and its own class's first example, and through the real
    ///     parser it was Malformed: «Member.Unresolved» accepts «x» as a
    ///     reference and the alternation commits before anything sees the arrow.
    ///     The unit test called <c>Delegate.Parse</c> directly over a token chain
    ///     it built itself, so it proved the component while the real path chose
    ///     a different one.
    ///     <para>
    ///     Safe to try first: <c>Delegate.Parse</c> works on a copy and assigns
    ///     the caller's parser only once it has the arrow AND a body, so a
    ///     «(x)» that turns out to be an input block costs one failed attempt
    ///     and nothing else.
    ///     </para>
    /// </remarks>
    public static new Value Parse(ref Parser current)
        => Delegate.Parse(ref current)
        ?? Member.Unresolved.Parse(ref current)
        ?? Temporary.Parse(ref current) as Value;
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
