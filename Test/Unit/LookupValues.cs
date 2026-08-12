// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Runtime;

namespace Unit;

/// <summary>
///     The runtime lookup value: admitted on the same boundary as a list, sorted
///     into a canonical order so that equal lookups are indistinguishable
///     downstream, keys canonicalised and errors refused at construction, and one
///     depth measure across both kinds.
/// </summary>
[Trait(nameof(Lookup), null)]
public class LookupValues
{
    private static KeyValuePair<object, object>[] Pairs(params (object Key, object Value)[] entries)
        => [.. entries.Select(entry => new KeyValuePair<object, object>(entry.Key, entry.Value))];

    private static KeyValuePair<object, object>[] Pairs(IEnumerable<(object Key, object Value)> entries)
        => [.. entries.Select(entry => new KeyValuePair<object, object>(entry.Key, entry.Value))];

    private static object Keyed(params (object Key, object Value)[] entries) => List.Admit(Pairs(entries));

    [Fact(DisplayName = "a pair-carrier is admitted as a lookup")]
    public void APairCarrierIsAdmittedAsALookup()
    {
        var lookup = Assert.IsType<Lookup>(Keyed(("a", 1d), ("b", 2d)));

        Assert.Equal(2, lookup.Count);
        Assert.Equal("a", lookup[0].Key);
        Assert.Equal("b", lookup[1].Key);
        Assert.Equal(2d, lookup[1].Value);
        Assert.Equal("[a = 1, b = 2]", lookup.ToString());
        Assert.Equal(["a", "b"], lookup.Select(entry => entry.Key));
    }

    [Fact(DisplayName = "equal lookups iterate identically, so cutoff cannot hide a change")]
    public void EqualLookupsIterateIdenticallySoCutoffCannotHideAChange()
    {
        // The two written orders are one canonical sequence, so equality ignoring
        // written order does not leave anything downstream able to tell them
        // apart. Insertion order kept for iteration would make cutoff suppress an
        // observable change — a wrong answer, not a missed optimisation.
        var written = Assert.IsType<Lookup>(Keyed(("b", 2d), ("a", 1d)));
        var reversed = Assert.IsType<Lookup>(Keyed(("a", 1d), ("b", 2d)));

        Assert.True(Builtin.Same(written, reversed));
        Assert.Equal(written.Select(entry => entry.Key), reversed.Select(entry => entry.Key));
        Assert.Equal(["a", "b"], written.Select(entry => entry.Key));

        // And the graph agrees: a «let» recomputed into the other order settles to
        // the same value, so nothing downstream of it is stale.
        Graph graph = new();

        graph.Var("reverse", false);
        graph.Let("table", scope => Equals(scope.Read("reverse"), true)
                                  ? Pairs(("b", 2d), ("a", 1d))
                                  : Pairs(("a", 1d), ("b", 2d)));
        graph.Let("first key", scope => ((Lookup)scope.Read("table"))[0].Key);

        graph.Write("reverse", true);
        graph.Step();

        Assert.Equal("a", ((Lookup)graph.Read("table"))[0].Key);
        Assert.Equal("a", graph.Read("first key"));
    }

    [Fact(DisplayName = "the order is zero exactly where the values are the same")]
    public void TheOrderIsZeroExactlyWhereTheValuesAreTheSame()
    {
        // The law canonicalisation rests on. Sorting produces one sequence per map
        // only if equal keys compare zero, and the duplicate refusal sees two equal
        // keys only if nothing unequal can sort between them.
        object[] keys =
        [
            Nothing.Instance, false, true, 0d, 1d, double.NaN, "", "1", new Instance("A", 1, 1), new Instance("A", 1, 2),
            new Error("same"), new Fault("same"), List.Admit(new object[] { 1d }), Keyed(("a", 1d)),
        ];

        foreach (var key in keys)
        {
            foreach (var other in keys)
            {
                var order = Lookup.Compare(key, other);

                Assert.Equal(Builtin.Same(key, other), order is 0);

                // Antisymmetric, so the sort cannot depend on which side it asked.
                Assert.Equal(order is 0 ? 0 : order < 0 ? -1 : 1, -System.Math.Sign(Lookup.Compare(other, key)));
            }
        }

        // An error and a fault reading alike are NOT one key — the kind is part of
        // the equality, so it has to be part of the order. Reached through a
        // compound key, since neither may be a key on its own.
        Assert.NotEqual(0, Lookup.Compare(List.Admit(new object[] { new Error("same") }),
                                          List.Admit(new object[] { new Fault("same") })));

        // «-0» and «0» are one value, and the order agrees rather than seating them
        // apart the way «CompareTo» alone would.
        Assert.Equal(0, Lookup.Compare(-0d, 0d));

        // Within a kind, by every part its equality has: an instance by type, then
        // slot, then generation.
        Assert.True(Lookup.Compare(new Instance("A", 1, 1), new Instance("B", 1, 1)) < 0);
        Assert.True(Lookup.Compare(new Instance("A", 1, 1), new Instance("A", 2, 1)) < 0);
        Assert.True(Lookup.Compare(new Instance("A", 1, 1), new Instance("A", 1, 2)) < 0);

        // An aggregate by length and then by parts.
        Assert.True(Lookup.Compare(List.Admit(new object[] { 1d }), List.Admit(new object[] { 1d, 2d })) < 0);
        Assert.True(Lookup.Compare(List.Admit(new object[] { 1d }), List.Admit(new object[] { 2d })) < 0);
        Assert.True(Lookup.Compare(Keyed(("a", 1d)), Keyed(("a", 1d), ("b", 2d))) < 0);
        Assert.True(Lookup.Compare(Keyed(("a", 1d)), Keyed(("b", 1d))) < 0);
        Assert.True(Lookup.Compare(Keyed(("a", 1d)), Keyed(("a", 2d))) < 0);

        // A pair of lists met twice down two paths is ordered once and remembered.
        var twice = new object[] { 1d };
        var beside = new object[] { 1d };

        Assert.Equal(0, Lookup.Compare(List.Admit(new object[] { twice, twice }),
                                       List.Admit(new object[] { beside, beside })));

        // A shared child inside a key is checked for orderability once, not once
        // per path that reaches it.
        var shared = new object[] { 1d };
        Assert.IsType<Lookup>(Keyed((new object[] { shared, shared }, 1d)));

        var alike = Pairs(("a", 1d));
        Assert.IsType<Lookup>(Keyed((Pairs(("l", alike), ("r", alike)), 1d)));
    }

    [Fact(DisplayName = "equal keys in either order canonicalise alike, and equal keys are refused however they print")]
    public void EqualKeysInEitherOrderCanonicaliseAlikeAndEqualKeysAreRefusedHoweverTheyPrint()
    {
        // Compound keys whose renderings do not order them: ordering by text put
        // two equal maps in opposite orders, so they compared unequal.
        object first = List.Admit(new object[] { 1d, 2d });
        object second = List.Admit(new object[] { 1d, 3d });

        Assert.True(Builtin.Same(Keyed((first, "x"), (second, "y")), Keyed((second, "y"), (first, "x"))));

        // And two keys that ARE the same land next to each other however they were
        // written, so the adjacent scan sees them — the duplicate cannot hide
        // behind an unequal key that happened to print between them.
        Assert.Contains("same key", Assert.IsType<Error>(
            Keyed((List.Admit(new object[] { 1d }), 1d),
                  (List.Admit(new object[] { 2d }), 2d),
                  (List.Admit(new object[] { 1d }), 3d))).Message);
    }

    [Fact(DisplayName = "a key the runtime cannot order is refused, and so is a bare null")]
    public void AKeyTheRuntimeCannotOrderIsRefusedAndSoIsABareNull()
    {
        // There is no deriving a content order for a host object's own equality
        // from its text, so it is refused rather than approximated — the
        // approximation admitted a map with two equal keys and two answers.
        Assert.Contains("can order", Assert.IsType<Error>(Keyed((System.DateTime.UnixEpoch, 1d))).Message);
        Assert.Contains("can order", Assert.IsType<Error>(Keyed((null, 1d))).Message);

        // Deep, because an aggregate is ordered by its parts.
        Assert.Contains("can order",
            Assert.IsType<Error>(Keyed((new object[] { 1d, System.DateTime.UnixEpoch }, 1d))).Message);
        Assert.Contains("can order",
            Assert.IsType<Error>(Keyed((Pairs(("k", System.DateTime.UnixEpoch)), 1d))).Message);

        // A lookup nested in a key is refused at its own admission, so an
        // unplaceable key never reaches the walk above.
        Assert.Contains("can order",
            Assert.IsType<Error>(Keyed((Pairs((System.DateTime.UnixEpoch, 1d)), 1d))).Message);

        // A VALUE is unrestricted still: only a key has to be placeable.
        Assert.IsType<Lookup>(Keyed(("k", System.DateTime.UnixEpoch)));

        // And every kind that IS orderable is admitted as a key.
        Assert.IsType<Lookup>(Keyed((Nothing.Instance, 1d), (true, 2d), (3d, 3d), ("s", 4d),
                                    (new Instance("A", 1, 1), 5d), (new object[] { 1d }, 6d), (Pairs(("a", 1d)), 7d)));
    }

    [Fact(DisplayName = "a fault used as a key stays a fault, and stays uncatchable")]
    public void AFaultUsedAsAKeyStaysAFaultAndStaysUncatchable()
    {
        // Refusing it would turn it into an ordinary error at the boundary, and an
        // ordinary error is caught — so an interpreter defect would become a
        // program value a program can swallow.
        var admitted = Assert.IsType<Fault>(Keyed((new Fault("interpreter defect"), 1d)));

        Assert.Equal("interpreter defect", admitted.Message);
        Assert.Same(admitted, Builtin.Otherwise(admitted, 9d));
    }

    [Fact(DisplayName = "a shared aggregate key is ordered once, not once per path")]
    public void ASharedAggregateKeyIsOrderedOnceNotOncePerPath()
    {
        // The sort runs BEFORE the duplicate check, so the exponential equality
        // was given a memo to stop arrives here through a different caller — and
        // testing equality alone cannot see it. Two independently admitted DAGs
        // whose every level mentions its child twice: forty layers is a million
        // million comparisons unfolded, and one per pair shared.
        static object Deep(int levels)
        {
            object built = 1d;

            for (var at = 0; at < levels; ++at) built = Pairs(("left", built), ("right", built));

            return List.Admit(built);
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Equal, so admission has to order them to find out and then refuse them.
        var refused = Assert.IsType<Error>(Keyed((Deep(40), "x"), (Deep(40), "y")));

        Assert.Contains("same key", refused.Message);
        Assert.True(stopwatch.Elapsed < System.TimeSpan.FromSeconds(10), $"took {stopwatch.Elapsed}");
    }

    /// <summary>A leaf that counts how many times it is compared.</summary>
    private sealed class Counted
    {
        public static int Comparisons;

        public override bool Equals(object other) { ++Comparisons; return other is Counted; }

        public override int GetHashCode() => 0;

        public override string ToString() => "counted";
    }

    [Fact(DisplayName = "a shared subtree is compared once, not once per path that reaches it")]
    public void ASharedSubtreeIsComparedOnceNotOncePerPathThatReachesIt()
    {
        // Admission keeps a repeated aggregate shared rather than expanding it,
        // so a comparison with no memory of the pairs it has proved re-proves each
        // shared child once per path that reaches it — 2^depth for a DAG whose
        // every level mentions its child twice. The depth ceiling is 256, so
        // construction does not bound that to anything usable.
        static object Deep(int levels)
        {
            object built = new Counted();

            for (var at = 0; at < levels; ++at) built = Pairs(("left", built), ("right", built));

            return List.Admit(built);
        }

        // Two INDEPENDENTLY admitted DAGs, so the shared child is a different
        // object on each side and reference equality cannot answer for it.
        var left = Deep(20);
        var right = Deep(20);

        Counted.Comparisons = 0;

        Assert.True(Builtin.Same(left, right));

        // Linear in the DAG, not in the tree it would unfold into: 2^20 is a
        // million, and the memo makes it one.
        Assert.InRange(Counted.Comparisons, 1, 64);
    }

    [Fact(DisplayName = "an error cannot be a lookup key, and can be a lookup value")]
    public void AnErrorCannotBeALookupKeyAndCanBeALookupValue()
    {
        // Two errors are equal when their reasons are, so admitting one as a key
        // would let two unrelated failures that printed alike become one entry —
        // a claim nobody would make on purpose.
        Assert.Contains("key cannot be an error", Assert.IsType<Error>(Keyed((new Error("boom"), 1d))).Message);
        Assert.Contains("boom", Assert.IsType<Error>(Keyed((new Error("boom"), 1d))).Message);

        // A VALUE is a different question, and an error is as legal there as it is
        // as a list element.
        var lookup = Assert.IsType<Lookup>(Keyed(("k", new Error("gone wrong"))));

        Assert.IsType<Error>(lookup[0].Value);
        Assert.IsType<List>(List.Admit(new object[] { new Error("gone wrong"), 2d }));
    }

    [Fact(DisplayName = "@ finds the association whose key is the index")]
    public void IndexingFindsTheAssociationWhoseKeyIsTheIndex()
    {
        var indexing = Builtin.Operators["@"];

        Assert.Equal(1d, indexing.Apply(Keyed(("a", 1d), ("b", 2d)), "a"));
        Assert.Equal(2d, indexing.Apply(Keyed(("a", 1d), ("b", 2d)), "b"));

        // A STRUCTURAL key is found by the same «is» equality that admitted it,
        // not by a host hash that would answer "not found" to a key that is one.
        Assert.Equal("x", indexing.Apply(Keyed((new object[] { 1d, 2d }, "x")), List.Admit(new object[] { 1d, 2d })));
        Assert.Equal("y", indexing.Apply(Keyed((Pairs(("a", 1d)), "y")), Keyed(("a", 1d))));

        // A MISS is an error rather than nothing, or «lookup of K (optional V)»
        // could not tell absent from present-and-nothing.
        Assert.Contains("no key", Assert.IsType<Error>(indexing.Apply(Keyed(("a", 1d)), "c")).Message);

        // And a list still indexes by position.
        Assert.Equal(2d, indexing.Apply(List.Admit(new object[] { 1d, 2d }), 2d));
        Assert.Contains("indexes a list or a lookup", Assert.IsType<Error>(indexing.Apply(1d, 1d)).Message);
    }

    [Fact(DisplayName = "a lookup is unordered for equality and its keys compare by value")]
    public void ALookupIsUnorderedForEqualityAndItsKeysCompareByValue()
    {
        // Order plays no part.
        Assert.True(Builtin.Same(Keyed(("a", 1d), ("b", 2d)), Keyed(("b", 2d), ("a", 1d))));

        // A different value under a shared key, a key one has and the other does
        // not, and a different count are each unequal.
        Assert.False(Builtin.Same(Keyed(("a", 1d)), Keyed(("a", 2d))));
        Assert.False(Builtin.Same(Keyed(("a", 1d), ("b", 2d)), Keyed(("a", 1d), ("c", 2d))));
        Assert.False(Builtin.Same(Keyed(("a", 1d)), Keyed(("a", 1d), ("b", 2d))));

        // The same reference is the same lookup without a walk.
        var lookup = Keyed(("a", 1d));
        Assert.True(Builtin.Same(lookup, lookup));

        // Keys are values, so two lookups differing only in the ORDER of a lookup
        // KEY are one key, and a value under it compares as itself.
        Assert.True(Builtin.Same(Keyed((Pairs(("x", 1d), ("y", 2d)), 9d)),
                                 Keyed((Pairs(("y", 2d), ("x", 1d)), 9d))));
    }

    [Fact(DisplayName = "two entries with the same key by value are refused")]
    public void TwoEntriesWithTheSameKeyByValueAreRefused()
    {
        Assert.Contains("same key", Assert.IsType<Error>(Keyed(("a", 1d), ("a", 2d))).Message);

        // By VALUE, not spelling: two lookup keys written in different orders are
        // one key, so a lookup with both has two answers and is refused.
        Assert.Contains("same key",
            Assert.IsType<Error>(Keyed((Pairs(("x", 1d), ("y", 2d)), 1d), (Pairs(("y", 2d), ("x", 1d)), 2d))).Message);
    }

    [Fact(DisplayName = "depth is one measure across both kinds, and a key counts toward it")]
    public void DepthIsOneMeasureAcrossBothKindsAndAKeyCountsTowardIt()
    {
        static object Nest(int layers, bool alternating)
        {
            object built = 1d;

            for (var at = 0; at < layers; ++at)
                built = alternating && at % 2 is 0 ? new object[] { built } : Pairs(("k", built));

            return List.Admit(built);
        }

        // A pure-lookup chain admits under the limit and refuses past it: each
        // layer adds one, the scalar at the root adds none.
        Assert.Equal(7, Assert.IsType<Lookup>(Nest(7, alternating: false)).Depth);
        Assert.True(Builtin.Same(Nest(20, false), Nest(20, false)));
        Assert.Contains("deeper", Assert.IsType<Error>(Nest(List.Deep + 4, false)).Message);

        // ALTERNATING list and lookup is refused at the COMBINED depth — a
        // per-kind counter would see each kind at half and admit it.
        Assert.Contains("deeper", Assert.IsType<Error>(Nest(List.Deep + 4, alternating: true)).Message);

        // A key counts toward depth exactly as an element does. Its depth
        // travels with the lookup — a counter that skipped keys would read this
        // as one — and a key at the limit refuses the shallow lookup that holds
        // it.
        object mid = 1d;
        for (var at = 0; at < 200; ++at) mid = new object[] { mid };
        Assert.Equal(201, Assert.IsType<Lookup>(Keyed((mid, 1d))).Depth);

        object key = 1d;
        for (var at = 0; at < List.Deep; ++at) key = new object[] { key };
        Assert.Contains("deeper", Assert.IsType<Error>(Keyed((key, 1d))).Message);

        // And a value, symmetrically.
        Assert.Contains("deeper", Assert.IsType<Error>(Keyed(("k", key))).Message);

        // An admitted lookup placed deeper than it fits is refused on reuse.
        var deep = Nest(List.Deep - 8, false);
        object under = deep;
        for (var at = 0; at < 12; ++at) under = new object[] { under };
        Assert.Contains("deeper", Assert.IsType<Error>(List.Admit(under)).Message);
    }

    [Fact(DisplayName = "a lookup cannot contain itself")]
    public void ALookupCannotContainItself()
    {
        var loop = new KeyValuePair<object, object>[1];
        loop[0] = new KeyValuePair<object, object>("k", loop);
        Assert.Contains("cannot contain itself", Assert.IsType<Error>(List.Admit(loop)).Message);

        // A cyclic key is refused the same way.
        var circular = new KeyValuePair<object, object>[1];
        circular[0] = new KeyValuePair<object, object>(circular, 1d);
        Assert.Contains("cannot contain itself", Assert.IsType<Error>(List.Admit(circular)).Message);
    }

    [Fact(DisplayName = "a lookup mentioned twice is shared, not expanded")]
    public void ALookupMentionedTwiceIsSharedNotExpanded()
    {
        var shared = Pairs(("a", 1d));
        var outer = Assert.IsType<Lookup>(Keyed(("x", shared), ("y", shared)));

        Assert.True(Builtin.Same(outer[0].Value, outer[1].Value));

        // An already-admitted lookup is admitted straight through when it is
        // placed inside another.
        var inner = Keyed(("a", 1d));
        var holding = Assert.IsType<Lookup>(Keyed(("held", inner)));
        Assert.Same(inner, holding[0].Value);
    }

    [Fact(DisplayName = "the empty lookup is a singleton, and it is not the empty list")]
    public void TheEmptyLookupIsASingletonAndItIsNotTheEmptyList()
    {
        Assert.Same(Lookup.Empty, List.Admit(Pairs()));

        // Different types, so a comparison is not a false so much as a category
        // apart — at runtime it answers false, and it is never confused with the
        // empty list.
        Assert.False(Builtin.Same(Lookup.Empty, List.Empty));
        Assert.True(Builtin.Same(Lookup.Empty, Lookup.Empty));
    }

    [Fact(DisplayName = "a lookup is not a list and not a scalar")]
    public void ALookupIsNotAListAndNotAScalar()
    {
        Assert.False(Builtin.Same(Keyed(("a", 1d)), List.Admit(new object[] { 1d })));
        Assert.False(Builtin.Same(Keyed(("a", 1d)), 1d));

        // A list may hold a lookup and a lookup a list — both count toward the
        // one depth.
        var list = Assert.IsType<List>(List.Admit(new object[] { Pairs(("a", 1d)) }));
        Assert.IsType<Lookup>(list[0]);

        var lookup = Assert.IsType<Lookup>(Keyed(("a", new object[] { 1d, 2d })));
        Assert.IsType<List>(lookup[0].Value);
    }
}
