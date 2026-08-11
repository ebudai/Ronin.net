// Copyright © 2026 Eric Budai

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Ronin.Runtime;

/// <summary>
///     A lookup, which is a value: immutable, compared by content, and held in a
///     canonical order so that equal lookups are the same sequence.
/// </summary>
///
/// <remarks>
///     <para>
///     A SEALED type beside <see cref="List"/> for the reason that file gives: a
///     list and a lookup need different equalities — a list is ordered as written
///     and a lookup as sorted — so they need different types to dispatch on,
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
///     CANONICAL ORDER, sorted at construction by a total order over keys, and the
///     written order is not recoverable. Order-insensitive equality beside
///     insertion-order iteration is unsound HERE in a way it is not in a language
///     without cutoff: a «let» recomputing «[a=1,b=2]» as «[b=2,a=1]» is unchanged
///     by equality, so cutoff fires and nothing downstream re-runs — while a «for
///     each» over it would have produced a different order. Cutoff would have
///     suppressed an observable change, which is a wrong answer rather than a
///     missed optimisation.
///     </para>
///     <para>
///     Sorting collapses the two decisions into one. Equal lookups iterate
///     identically, so nothing downstream can tell them apart; and lookup equality
///     becomes the LIST comparison applied to a canonical form rather than a second
///     function that can disagree with the first. It costs one sort per lookup,
///     paid once, because a lookup is immutable.
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
    ///     The total order two keys are sorted into, so that equal lookups are the
    ///     same sequence.
    /// </summary>
    ///
    /// <remarks>
    ///     BY KIND first and then by content, because the order has to be total
    ///     across every value a key can be — one written «1» and another «"1"» are
    ///     different keys and something has to put them either side of each other.
    ///     Every case is content-derived, so it is the same on every run: an
    ///     instance is its type, slot and generation, which is what its identity
    ///     already is, and an aggregate is its length and then its parts, which is
    ///     the same walk equality makes. Nothing here consults a hash or an address.
    /// </remarks>
    public static int Compare(object key, object other)
    {
        var kind = Kind(key).CompareTo(Kind(other));

        if (kind is not 0) return kind;

        return (key, other) switch
        {
            (bool first, bool second) => first.CompareTo(second),
            (double first, double second) => first.CompareTo(second),
            (string first, string second) => string.CompareOrdinal(first, second),
            (Instance first, Instance second) => Ordered(first, second),
            (Error first, Error second) => string.CompareOrdinal(first.Message, second.Message),
            (List first, List second) => Ordered(first, second),
            (Lookup first, Lookup second) => Ordered(first, second),

            // Nothing is the only one of its kind, and a host value the runtime
            // has no order for is ordered by what it prints — deterministic,
            // which is all a canonical form asks of it.
            _ => string.CompareOrdinal(key?.ToString(), other?.ToString()),
        };
    }

    private static int Ordered(Instance first, Instance second)
    {
        var type = string.CompareOrdinal(first.Type, second.Type);

        if (type is not 0) return type;

        var slot = first.Slot.CompareTo(second.Slot);

        return slot is not 0 ? slot : first.Generation.CompareTo(second.Generation);
    }

    private static int Ordered(List first, List second)
    {
        var length = first.Count.CompareTo(second.Count);

        if (length is not 0) return length;

        for (var at = 0; at < first.Count; ++at)
        {
            var element = Compare(first[at], second[at]);

            if (element is not 0) return element;
        }

        return 0;
    }

    private static int Ordered(Lookup first, Lookup second)
    {
        var length = first.Count.CompareTo(second.Count);

        if (length is not 0) return length;

        // Both are canonical already, so this walks two sorted sequences and the
        // first place they differ is the answer.
        for (var at = 0; at < first.Count; ++at)
        {
            var key = Compare(first[at].Key, second[at].Key);

            if (key is not 0) return key;

            var value = Compare(first[at].Value, second[at].Value);

            if (value is not 0) return value;
        }

        return 0;
    }

    /// <summary>Which kind a key is, which is the first half of the order.</summary>
    private static int Kind(object key) => key switch
    {
        Nothing => 0,
        bool => 1,
        double => 2,
        string => 3,
        Instance => 4,
        Error => 5,
        List => 6,
        Lookup => 7,
        _ => 8,
    };

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

    /// <summary>The entry at a position in the canonical order every equal lookup shares.</summary>
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
