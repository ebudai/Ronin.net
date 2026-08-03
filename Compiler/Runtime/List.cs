// Copyright © 2026 Eric Budai

using System.Collections;
using System.Collections.Generic;

namespace Ronin.Runtime;

/// <summary>
///     A list, which is a value: immutable, ordered, and compared by content.
/// </summary>
///
/// <remarks>
///     <para>
///     A SEALED type and not <c>ImmutableArray&lt;object&gt;</c>, and the reason
///     is the language rather than the implementation. A list and a lookup need
///     different equalities — a list is ordered and a lookup is not — so they
///     need different types for equality to dispatch on, and «x is a list» needs
///     a type to answer from. One CLR type for both would need a tag beside the
///     value, and a tag that can be wrong is another representation defect
///     waiting.
///     </para>
///     <para>
///     The storage is private, so it cannot be recovered by casting. A
///     read-only interface over a caller's array would satisfy that test and fix
///     nothing, because the caller still holds the array — which is the trap:
///     it looks like the repair while leaving the invariant asserted and false.
///     </para>
///     <para>
///     When a bulk path arrives — a vectorised sum, a copy into a column — it
///     should take a <c>ReadOnlySpan&lt;object&gt;</c> rather than an accessor
///     handing out the array, because a span cannot be stored in a field and so
///     non-retention is checked rather than promised. Not written yet: an
///     accessor with no caller is the thing that loses the invariant the first
///     time something needs to be fast, so it arrives with its first user.
///     </para>
/// </remarks>
internal sealed class List : IReadOnlyList<object>
{
    private List(object[] values, int depth)
    {
        this.values = values;
        Depth = depth;
    }

    /// <summary>
    ///     The empty list, which is the commonest one in any program.
    /// </summary>
    ///
    /// <remarks>
    ///     A singleton and not an intern table. Interning was refused because a
    ///     global table is never collected in an always-running session and is a
    ///     synchronisation point in a threading design built to have none. One
    ///     static is none of those things — no lookup, no growth, no contention —
    ///     and it makes the commonest equality O(1).
    /// </remarks>
    public static List Empty { get; } = new([], 1);

    public int Count => values.Length;

    public object this[int index] => values[index];

    /// <summary>
    ///     The list of <paramref name="elements"/>, deep-copied.
    /// </summary>
    ///
    /// <remarks>
    ///     DEEP, because the two cheaper readings of "normalise" preserve the
    ///     defect. Wrapping the caller's array leaves the caller holding it, and
    ///     copying only the top level leaves the same hole one level down —
    ///     which is not exotic, since nested lists arrive with grouped data and
    ///     with match arms.
    /// </remarks>
    public static object Of(object value)
    {
        var normalised = Normalise(value, [], 0);

        return normalised is Refusal refused ? new Error(refused.Reason) : normalised;
    }

    /// <summary>How deep a value may nest, so that comparing one always ends.</summary>
    ///
    /// <remarks>
    ///     Refused at CONSTRUCTION rather than capped at comparison. A cap in the
    ///     comparison made two accepted equal lists compare unequal, which is not
    ///     an equivalence — and it is observable, because cutoff, «changes» and
    ///     «old» all ask that question and «is» will. A value the runtime accepts
    ///     must be one it can compare honestly, so the limit belongs where the
    ///     value is admitted.
    /// </remarks>
    public const int Deep = 256;

    /// <summary>
    ///     How far this nests, which travels with it.
    /// </summary>
    ///
    /// <remarks>
    ///     Carried rather than counted per call, because a counter that starts
    ///     at zero on each call is bypassed one layer at a time: wrapping an
    ///     already-deep list in a new array would begin counting again.
    /// </remarks>
    public int Depth { get; }

    /// <summary>A refusal, which is not a value and must not be mistaken for one.</summary>
    ///
    /// <remarks>
    ///     Its own type, because the previous sentinel was an <see cref="Error"/>
    ///     and an error IS a value here. So «[ 1 / 0, 2 ]» stopped being a list
    ///     with a failed element and became the failure — a semantic change
    ///     nobody asked for, obtained accidentally by a cycle check that could
    ///     not tell its own report from the thing it was copying.
    /// </remarks>
    private sealed class Refusal(string reason)
    {
        public string Reason { get; } = reason;
    }

    private static object Normalise(object value, HashSet<object> inside, int depth)
    {
        if (value is List already)
            return depth + already.Depth > Deep ? TooDeep() : already;

        if (value is not object[] array) return value;
        if (array.Length is 0) return Empty;
        if (depth >= Deep) return TooDeep();

        // Here, and not in the comparison. A cycle can only be reported usefully
        // where the value still has a name and a caller: at the boundary the
        // message can say which argument contains itself, where a comparison
        // sees two anonymous values and can only say "too deep".
        if (inside.Add(array) is false)
            return new Refusal("a list cannot contain itself. Copy the part that repeats, or hold it by name.");

        var copied = new object[array.Length];
        var deepest = 0;

        for (var at = 0; at < array.Length; ++at)
        {
            copied[at] = Normalise(array[at], inside, depth + 1);

            // A REFUSAL stops it. An ordinary error is an element like any
            // other and the list keeps it.
            if (copied[at] is Refusal) return copied[at];

            if (copied[at] is List nested && nested.Depth > deepest) deepest = nested.Depth;
        }

        inside.Remove(array);

        return new List(copied, deepest + 1);
    }

    private static Refusal TooDeep()
        => new($"a list may nest {Deep.ToString(System.Globalization.CultureInfo.InvariantCulture)} deep, and " +
               "this one is deeper. Nothing can compare it, so nothing can tell whether it changed.");

    public override string ToString() => $"[{string.Join(", ", values)}]";

    public IEnumerator<object> GetEnumerator() => ((IEnumerable<object>)values).GetEnumerator();

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    IEnumerator IEnumerable.GetEnumerator() => values.GetEnumerator();

    private readonly object[] values;
}
