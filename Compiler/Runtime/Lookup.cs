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
///     its keys are distinct and its depth is measured on the same traversal as a
///     list's — a lookup is never constructed beside that, where a second depth
///     counter would admit a value twice as deep as the comparison was sized to
///     follow.
///     </para>
///     <para>
///     INSERTION ORDER is preserved for iteration and ignored for equality. Two
///     lookups may therefore be equal and iterate differently, and that is the
///     named trade rather than an oversight: iteration and display want
///     DETERMINISM, which insertion order already gives, while equality wants the
///     map — the same keys with the same value at each — which order has no part
///     in.
///     </para>
///     <para>
///     A CANONICAL order was tried in its place, to make equal lookups iterate
///     alike, and it is deleted. Nothing needed a total order: equality answers
///     «is», the duplicate refusal, cutoff, «old», «changes» and indexing, and the
///     two consumers that are left want determinism rather than order. What an
///     order costs is an obligation to place every kind against every other, for
///     ever, including kinds nobody has added yet — and the first attempt paid it
///     by ordering unknown values on their printed text, which seated unequal keys
///     together and equal keys apart. An equality over admitted values is
///     structural and derivable; an order over them is not, and a hash consistent
///     with equality is what accelerates the duplicate scan if one ever needs it.
///     </para>
/// </remarks>
internal sealed class Lookup : IReadOnlyList<KeyValuePair<object, object>>
{
    private Lookup(KeyValuePair<object, object>[] entries, int depth)
    {
        this.entries = entries;
        Depth = depth;
    }

    /// <summary>An admitted lookup, its keys already distinct and its depth measured.</summary>
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

    /// <summary>The entry at a position in iteration order, which is insertion order.</summary>
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
