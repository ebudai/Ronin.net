// Copyright © 2026 Eric Budai

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Ronin.Runtime;

/// <summary>
///     A lookup, which is a value: immutable, compared by content, and unordered
///     for equality while ordered for iteration.
/// </summary>
///
/// <remarks>
///     <para>
///     A SEALED type beside <see cref="List"/> for the reason that file gives: a
///     list and a lookup need different equalities — a list is ordered and a
///     lookup is not — so they need different types for equality to dispatch on,
///     and «x is a lookup» needs a type to answer from. A tag beside one value
///     type for both is a tag that can be wrong.
///     </para>
///     <para>
///     Built only through <see cref="List.Admit"/>, the one admission boundary, so
///     its keys are canonicalised and its depth is measured on the same traversal
///     as a list's — a lookup is never constructed beside that, where a second
///     depth counter would admit a value twice as deep as the comparison was sized
///     to follow.
///     </para>
///     <para>
///     INSERTION ORDER is preserved for iteration and ignored for equality. Two
///     lookups may be equal and iterate differently: equality is the set of
///     key-value pairs, and a «for each» in an always-running IDE must not vary
///     between runs or a program stops being reproducible. That is the one
///     surprising thing about the type, and it is the right trade.
///     </para>
/// </remarks>
internal sealed class Lookup : IReadOnlyList<KeyValuePair<object, object>>
{
    private Lookup(KeyValuePair<object, object>[] entries, int depth)
    {
        this.entries = entries;
        Depth = depth;
    }

    /// <summary>An admitted lookup, its keys already canonicalised and its depth measured.</summary>
    internal static Lookup Of(KeyValuePair<object, object>[] entries, int depth) => new(entries, depth);

    /// <summary>
    ///     The empty lookup.
    /// </summary>
    ///
    /// <remarks>
    ///     A singleton beside <see cref="List.Empty"/> for the same reasons, and
    ///     NOT equal to it: the two have different types, so a comparison between
    ///     them is a type error rather than a false. «[]» is the empty list; the
    ///     empty lookup has no literal of its own and arrives from the expected
    ///     type, so this is reached by that path rather than by a second spelling.
    /// </remarks>
    public static Lookup Empty { get; } = new([], 1);

    public int Count => entries.Length;

    /// <summary>The entry at a position in ITERATION order, which is insertion order.</summary>
    public KeyValuePair<object, object> this[int index] => entries[index];

    /// <summary>How far this nests, which travels with it, one measure shared with <see cref="List"/>.</summary>
    public int Depth { get; }

    public override string ToString() => $"[{string.Join(", ", entries.Select(entry => $"{entry.Key} = {entry.Value}"))}]";

    public IEnumerator<KeyValuePair<object, object>> GetEnumerator()
        => ((IEnumerable<KeyValuePair<object, object>>)entries).GetEnumerator();

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    IEnumerator IEnumerable.GetEnumerator() => entries.GetEnumerator();

    private readonly KeyValuePair<object, object>[] entries;
}
