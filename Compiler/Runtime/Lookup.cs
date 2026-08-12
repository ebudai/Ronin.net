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
    ///     Whether a key is one this order can place — every part of it, all the
    ///     way down.
    /// </summary>
    ///
    /// <remarks>
    ///     A key must satisfy «Compare is zero exactly when the values are the
    ///     same», because that law is the whole of what makes sorting produce one
    ///     sequence per map and what puts two equal keys next to each other for the
    ///     duplicate refusal. There is no way to derive it for an arbitrary host
    ///     object: its «Equals» is its own and its text is neither an identity nor
    ///     an order — two unequal keys may print alike, two equal ones differently.
    ///     Ordering by what a value printed did both, and admitted a map with two
    ///     equal keys and two answers.
    ///     <para>
    ///     So a key the runtime has no content order for is REFUSED rather than
    ///     approximated, and a bare CLR null with it. Deep, because an aggregate is
    ///     a legal key and is ordered by its parts, so one unorderable part makes
    ///     the whole of it unorderable. Shared children are visited once, which is
    ///     what keeps the check linear over a DAG.
    ///     </para>
    /// </remarks>
    public static bool Orderable(object key) => Orderable(key, []);

    private static bool Orderable(object key, HashSet<object> seen) => key switch
    {
        Nothing or bool or double or string or Instance or Error => true,
        List list => seen.Add(list) is false || list.All(part => Orderable(part, seen)),
        // A lookup's own KEYS were refused at its admission if they could not be
        // placed, so only its values are still in question here. A list's elements
        // are unrestricted and are.
        Lookup lookup => seen.Add(lookup) is false || lookup.All(entry => Orderable(entry.Value, seen)),
        _ => false,
    };

    /// <summary>
    ///     The total order two keys are sorted into, so that equal lookups are the
    ///     same sequence.
    /// </summary>
    ///
    /// <remarks>
    ///     BY KIND first and then by content, because the order has to be total
    ///     across every value a key can be — one written «1» and another «"1"» are
    ///     different keys and something has to put them either side of each other.
    ///     Every case is content-derived and covers everything that value's
    ///     equality covers, so this is zero exactly where <see cref="Builtin.Same"/>
    ///     is true: an instance is its type, slot and generation; an error is its
    ///     kind and its reason, because a fault is not the error that reads alike;
    ///     an aggregate is its length and then its parts. Nothing consults a hash,
    ///     an address, or a rendering.
    /// </remarks>
    public static int Compare(object key, object other)
        => Compare(key, other, key is List or Lookup ? [] : null);

    /// <param name="compared">
    ///     The aggregate pairs already ordered, and what they came to.
    ///     <para>
    ///     Admission keeps a repeated aggregate shared rather than expanding it, so
    ///     a comparison with no memory re-orders each shared child once per path
    ///     that reaches it — the same exponential the equality beside this one was
    ///     given a memo to stop, and it arrives here through the sort that runs
    ///     before the duplicate check. The RESULT is kept and not merely the fact
    ///     of having been here, because an order is three answers rather than two
    ///     and another path wants the one already proved.
    ///     </para>
    ///     <para>
    ///     Null exactly when nothing can recurse: a scalar key never reaches an
    ///     aggregate, so it pays for no table.
    ///     </para>
    /// </param>
    private static int Compare(object key, object other, Dictionary<(object Left, object Right), int> compared)
    {
        var kind = Kind(key).CompareTo(Kind(other));

        if (kind is not 0) return kind;

        return Kind(key) switch
        {
            // Nothing is the only one of its kind, so being of it is the whole
            // answer.
            0 => 0,
            1 => ((bool)key).CompareTo((bool)other),
            2 => Ordered((double)key, (double)other),
            3 => string.CompareOrdinal((string)key, (string)other),
            4 => Ordered((Instance)key, (Instance)other),
            5 => Ordered((Error)key, (Error)other),
            6 => Ordered((List)key, (List)other, compared),
            _ => Ordered((Lookup)key, (Lookup)other, compared),
        };
    }

    /// <remarks>
    ///     Equality first, because «CompareTo» separates «-0» from «0» where
    ///     equality joins them — and it is equality this has to agree with. It is
    ///     «CompareTo» afterwards rather than «&lt;», so that two values that are
    ///     not the same never come to zero, which is what a NaN beside a number
    ///     would otherwise do.
    /// </remarks>
    private static int Ordered(double first, double second)
        => first.Equals(second) ? 0 : first.CompareTo(second);

    private static int Ordered(Instance first, Instance second)
    {
        var type = string.CompareOrdinal(first.Type, second.Type);

        if (type is not 0) return type;

        var slot = first.Slot.CompareTo(second.Slot);

        return slot is not 0 ? slot : first.Generation.CompareTo(second.Generation);
    }

    /// <remarks>
    ///     The KIND and then the reason, because that is what an error's equality
    ///     is: a fault and an error carrying one message are not the same value,
    ///     and an order that called them one would seat two unequal keys together.
    /// </remarks>
    private static int Ordered(Error first, Error second)
    {
        var kind = string.CompareOrdinal(first.GetType().Name, second.GetType().Name);

        return kind is not 0 ? kind : string.CompareOrdinal(first.Message, second.Message);
    }

    private static int Ordered(List first, List second, Dictionary<(object Left, object Right), int> compared)
    {
        if (ReferenceEquals(first, second)) return 0;
        if (compared.TryGetValue((first, second), out var already)) return already;

        var length = first.Count.CompareTo(second.Count);

        if (length is not 0) return compared[(first, second)] = length;

        for (var at = 0; at < first.Count; ++at)
        {
            var element = Compare(first[at], second[at], compared);

            if (element is not 0) return compared[(first, second)] = element;
        }

        return compared[(first, second)] = 0;
    }

    private static int Ordered(Lookup first, Lookup second, Dictionary<(object Left, object Right), int> compared)
    {
        if (ReferenceEquals(first, second)) return 0;
        if (compared.TryGetValue((first, second), out var already)) return already;

        var length = first.Count.CompareTo(second.Count);

        if (length is not 0) return compared[(first, second)] = length;

        // Both are canonical already, so this walks two sorted sequences and the
        // first place they differ is the answer.
        for (var at = 0; at < first.Count; ++at)
        {
            var key = Compare(first[at].Key, second[at].Key, compared);

            if (key is not 0) return compared[(first, second)] = key;

            var value = Compare(first[at].Value, second[at].Value, compared);

            if (value is not 0) return compared[(first, second)] = value;
        }

        return compared[(first, second)] = 0;
    }

    /// <summary>Which kind a key is, which is the first half of the order.</summary>
    ///
    /// <remarks>
    ///     Every kind <see cref="Orderable"/> admits, and no other: an unrecognised
    ///     value never reaches here, because a key that is not orderable is refused
    ///     where the depth and the cycle are.
    /// </remarks>
    private static int Kind(object key) => key switch
    {
        Nothing => 0,
        bool => 1,
        double => 2,
        string => 3,
        Instance => 4,
        Error => 5,
        List => 6,
        _ => 7,
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
