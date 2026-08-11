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
    ///     <paramref name="value"/> in the form the runtime holds: THE admission
    ///     boundary, and every value-bearing API crosses it.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     Named for the boundary rather than for lists, because it was called
    ///     «Of» and read as a list constructor — so each API that took an
    ///     «object» from a caller had to remember a call that looked like it was
    ///     about something else. «Var», two writes, body results, evaluator
    ///     groups, type seeds, declaration input and output, and constants were
    ///     found by eight successive sweeps, one door at a time.
    ///     </para>
    ///     <para>
    ///     DEEP, because the two cheaper readings of "normalise" preserve the
    ///     defect. Wrapping the caller's array leaves the caller holding it, and
    ///     copying only the top level leaves the same hole one level down —
    ///     which is not exotic, since nested lists arrive with grouped data and
    ///     with match arms.
    ///     </para>
    ///     <para>
    ///     Anything that is not an array is returned unchanged, so this is safe
    ///     to call on every value rather than only on the ones expected to be
    ///     lists — which is the point: an API cannot be wrong about whether it
    ///     needs it.
    ///     </para>
    /// </remarks>
    public static object Admit(object value)
    {
        // Before the traversal state exists. Nothing but a raw array needs a
        // path set or a completed-node map, and almost nothing IS one: a scalar,
        // a text, an error and an already-admitted list are what the reactive
        // hot path admits, and each was paying 144 bytes for machinery it never
        // reached. That is the price of making the call universal, which is the
        // right shape — so the call has to be free when there is nothing to do.
        //
        // An admitted list is returned as it stands rather than measured,
        // because nothing admitted is deeper than the limit; the depth question
        // only arises where one is placed INSIDE something else.
        if (value is not (object[] or KeyValuePair<object, object>[])) return value;

        var normalised = Normalise(value, [], new Dictionary<object, object>(ReferenceEqualityComparer.Instance), 0);

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

    private static object Normalise(object value, HashSet<object> inside, Dictionary<object, object> done, int depth)
    {
        if (value is List already) return Fits(already, depth);

        if (value is Lookup keyed) return Fits(keyed, depth);

        if (value is KeyValuePair<object, object>[] pairs) return Associated(pairs, inside, done, depth);

        if (value is not object[] array) return value;

        // A SECOND reference to one array is the same value, not another copy of
        // it. Without this a host DAG — one array per level, mentioned twice —
        // was expanded into a tree: «inside» drops a child once it completes, so
        // the next mention rebuilt the whole subtree. Sixteen input arrays
        // allocated 8.9 MB, and two dozen reach gigabytes, all of it acyclic and
        // far inside the depth limit.
        //
        // Reusing the completed child is invisible: a list is a value, identity
        // is not its equality, and it cannot be mutated to make the sharing
        // observable.
        if (done.TryGetValue(array, out var admitted)) return Fits(admitted, depth);

        // Before the empty case and not after it. «[]» took the early return
        // and skipped this, so a nest of exactly «Deep» wrappers around an empty
        // list was admitted at depth 257 — past the limit that is supposed to
        // define what the runtime accepts.
        if (depth >= Deep) return TooDeep();

        if (array.Length is 0) return Empty;

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
            copied[at] = Normalise(array[at], inside, done, depth + 1);

            // A REFUSAL stops it. An ordinary error is an element like any
            // other and the list keeps it.
            if (copied[at] is Refusal) return copied[at];

            deepest = System.Math.Max(deepest, Nesting(copied[at]));
        }

        inside.Remove(array);

        return done[array] = new List(copied, deepest + 1);
    }

    /// <summary>
    ///     A lookup in the form the runtime holds, admitted on the SAME traversal
    ///     as a list — one <paramref name="inside"/> set, one <paramref name="done"/>
    ///     map, one depth.
    /// </summary>
    ///
    /// <remarks>
    ///     A key counts toward the depth exactly as an element does, because a
    ///     value alternating list and lookup is twice as deep as either counter
    ///     sees if each kind counts only its own — and the limit exists so the
    ///     value can be compared, which does not care which kind each layer was.
    /// </remarks>
    private static object Associated(KeyValuePair<object, object>[] pairs, HashSet<object> inside,
                                     Dictionary<object, object> done, int depth)
    {
        if (done.TryGetValue(pairs, out var admitted)) return Fits(admitted, depth);

        if (depth >= Deep) return TooDeep();

        if (pairs.Length is 0) return Lookup.Empty;

        if (inside.Add(pairs) is false)
            return new Refusal("a lookup cannot contain itself. Copy the part that repeats, or hold it by name.");

        var entries = new KeyValuePair<object, object>[pairs.Length];
        var deepest = 0;

        for (var at = 0; at < pairs.Length; ++at)
        {
            var key = Normalise(pairs[at].Key, inside, done, depth + 1);

            if (key is Refusal) return key;

            var value = Normalise(pairs[at].Value, inside, done, depth + 1);

            if (value is Refusal) return value;

            // Canonicalised at construction: two keys equal by VALUE are one key
            // whatever their spelling, and a lookup with two of them has two
            // answers and no reason to prefer either. Refused here the way a cycle
            // is, and by an exact comparison — a capped one would call two unequal
            // keys the same, which is observable through cutoff and «old».
            for (var prior = 0; prior < at; ++prior)
                if (Builtin.Same(entries[prior].Key, key))
                    return new Refusal("two entries of a lookup have the same key, so a lookup of it has two " +
                                       "answers and no reason to prefer either. Remove one, or give them different keys.");

            entries[at] = new KeyValuePair<object, object>(key, value);
            deepest = System.Math.Max(deepest, System.Math.Max(Nesting(key), Nesting(value)));
        }

        inside.Remove(pairs);

        return done[pairs] = Lookup.Of(entries, deepest + 1);
    }

    /// <summary>An already-admitted list, if it still fits where it is going.</summary>
    ///
    /// <remarks>
    ///     Asked again on reuse because depth is a property of the WHOLE value:
    ///     a list admitted at the limit is fine on its own and too deep one
    ///     level down, and it arrives at both through this.
    /// </remarks>
    private static object Fits(object value, int depth) => depth + Nesting(value) > Deep ? TooDeep() : value;

    /// <summary>How far an admitted value nests, one measure across both aggregate kinds.</summary>
    private static int Nesting(object value) => value switch
    {
        List list => list.Depth,
        Lookup lookup => lookup.Depth,
        _ => 0,
    };

    private static Refusal TooDeep()
        => new($"a value may nest {Deep.ToString(System.Globalization.CultureInfo.InvariantCulture)} deep, and " +
               "this one is deeper. Nothing can compare it, so nothing can tell whether it changed.");

    public override string ToString() => $"[{string.Join(", ", values)}]";

    public IEnumerator<object> GetEnumerator() => ((IEnumerable<object>)values).GetEnumerator();

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    IEnumerator IEnumerable.GetEnumerator() => values.GetEnumerator();

    private readonly object[] values;
}
