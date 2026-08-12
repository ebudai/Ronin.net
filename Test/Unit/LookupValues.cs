// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Runtime;

namespace Unit;

/// <summary>
///     The runtime lookup value: admitted on the same boundary as a list, held in
///     the order it was written and compared as a map that ignores that order,
///     with duplicate keys and kinds the runtime does not know refused at
///     construction, and one depth measure across both kinds.
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

    [Fact(DisplayName = "a lookup iterates as written and compares as a map, which is the named trade")]
    public void ALookupIteratesAsWrittenAndComparesAsAMapWhichIsTheNamedTrade()
    {
        var written = Assert.IsType<Lookup>(Keyed(("b", 2d), ("a", 1d)));
        var reversed = Assert.IsType<Lookup>(Keyed(("a", 1d), ("b", 2d)));

        // Equal, because a lookup is a map: the same keys with the same value at
        // each, and the order they were written in is not part of the value.
        Assert.True(Builtin.Same(written, reversed));

        // And iterated as written, because iteration wants determinism rather than
        // an order, and insertion order is deterministic.
        Assert.Equal(["b", "a"], written.Select(entry => entry.Key));
        Assert.Equal(["a", "b"], reversed.Select(entry => entry.Key));

        // THE TRADE, through the graph, written down because it is the surprising
        // half: a «let» recomputed into the other order is the same VALUE, so the
        // reorder is not a change and the clock does not move for it — while a
        // walk over the result can still see the difference. That is the price of
        // an equality that ignores something a program can look at, and it is
        // accepted rather than overlooked. A canonical order was tried to remove
        // it and cost more than it saved, since nothing needed a total order.
        Graph graph = new();

        graph.Var("reverse", false);
        graph.Let("table", scope => Equals(scope.Read("reverse"), true)
                                  ? Pairs(("b", 2d), ("a", 1d))
                                  : Pairs(("a", 1d), ("b", 2d)));
        graph.Let("first key", scope => ((Lookup)scope.Read("table"))[0].Key);

        // READ FIRST, because a «let» is lazy: until one is read it has never run,
        // has no dependency edge, and there is nothing cached for cutoff to keep.
        // A test that writes before reading measures a first evaluation rather
        // than a suppressed one, and would pass whatever cutoff did.
        Assert.Equal("a", graph.Read("first key"));

        graph.Write("reverse", true);
        graph.Step();

        // The table recomputed into the other order and a walk over it sees that
        // order — while equality calls it the same value, so the change clock does
        // not move and the dependent keeps what it cached. Two lookups equal and
        // walked differently, which is the trade, showing here as a dependent that
        // does not wake.
        Assert.Equal("b", ((Lookup)graph.Read("table"))[0].Key);
        Assert.Equal("a", graph.Read("first key"));
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
            object built = 1d;

            for (var at = 0; at < levels; ++at) built = Pairs(("left", built), ("right", built));

            return List.Admit(built);
        }

        // Two INDEPENDENTLY admitted DAGs, so the shared child is a different
        // object on each side and reference equality cannot answer for it. Twenty
        // layers unfold into a million comparisons without the memo and twenty
        // with it, so the clock is the assertion that can tell them apart.
        var left = Deep(20);
        var right = Deep(20);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        Assert.True(Builtin.Same(left, right));

        Assert.True(stopwatch.Elapsed < System.TimeSpan.FromSeconds(5), $"took {stopwatch.Elapsed}");

        // The same pair of lookups reached down two different keys is proved once
        // and remembered, which is what makes the walk above linear.
        var child = Pairs(("a", 1d));

        Assert.True(Builtin.Same(Keyed(("p", child), ("q", child)), Keyed(("p", child), ("q", child))));

        // And the same for a pair of lists met twice, since the memo is one across
        // both kinds rather than one per kind.
        var element = new object[] { 1d };

        Assert.True(Builtin.Same(List.Admit(new object[] { element, element }),
                                 List.Admit(new object[] { element, element })));
    }

    [Fact(DisplayName = "a value of a kind the runtime does not know is refused, as a key and as a value")]
    public void AValueOfAKindTheRuntimeDoesNotKnowIsRefusedAsAKeyAndAsAValue()
    {
        // At the BOUNDARY and in every position. Admission exists to make "a value
        // the runtime accepts must be one it can compare honestly" true, and a
        // host object carrying its own equality cannot be — so refusing it only in
        // key position would leave it legal inside a list then used as one, which
        // is the same hole one level out.
        Assert.Contains("no value of this kind", Assert.IsType<Error>(List.Admit(System.DateTime.UnixEpoch)).Message);
        Assert.Contains("no value of this kind", Assert.IsType<Error>(Keyed((System.DateTime.UnixEpoch, 1d))).Message);
        Assert.Contains("no value of this kind", Assert.IsType<Error>(Keyed(("k", System.DateTime.UnixEpoch))).Message);
        Assert.Contains("no value of this kind",
            Assert.IsType<Error>(List.Admit(new object[] { 1d, System.DateTime.UnixEpoch })).Message);
        Assert.Contains("no value of this kind",
            Assert.IsType<Error>(Keyed((new object[] { System.DateTime.UnixEpoch }, 1d))).Message);

        // A whole number written as one is a host integer and not the language's
        // number, which is a double — and it would compare unequal to every number
        // beside it, so it is refused rather than quietly stored.
        Assert.Contains("no value of this kind", Assert.IsType<Error>(List.Admit(1)).Message);

        // Every kind it DOES know passes, in either position.
        Assert.IsType<Lookup>(Keyed((Nothing.Instance, 1d), (true, 2d), (3d, 3d), ("s", 4d),
                                    (new Instance("A", 1, 1), 5d), (new object[] { 1d }, 6d), (Pairs(("a", 1d)), 7d)));

        Assert.Equal(1d, List.Admit(1d));
        Assert.IsType<List>(List.Admit(new object[] { new Error("kept"), Nothing.Instance, true, "s" }));
    }

    [Fact(DisplayName = "a bare null is a fault, because the language's no-value is nothing")]
    public void ABareNullIsAFaultBecauseTheLanguagesNoValueIsNothing()
    {
        // Not an error and not an ordering question: a null arriving here is the
        // interpreter having gone wrong above, so it must not be catchable.
        var absent = Assert.IsType<Fault>(List.Admit(null));

        Assert.Contains("may not be absent", absent.Message);
        Assert.Same(absent, Builtin.Otherwise(absent, 9d));

        // And it is not buried inside an aggregate either, because a fault is the
        // one failure that is not a value.
        Assert.IsType<Fault>(List.Admit(new object[] { 1d, null }));
        Assert.IsType<Fault>(Keyed((null, 1d)));
        Assert.IsType<Fault>(Keyed(("k", null)));
    }

    [Fact(DisplayName = "a key candidate that failed leaves nothing behind to make a later one pass")]
    public void AKeyCandidateThatFailedLeavesNothingBehindToMakeALaterOnePass()
    {
        // Unordered matching is the one place a comparison continues after a
        // false, and the memo records a pair before proving it — so pairs explored
        // down a candidate that then failed would stay behind, and the next key
        // meeting one of them would be told it was already proved.
        object first = new object[] { 1d, 2d };
        object second = new object[] { 1d, 3d };

        // The maps differ in exactly one association: one holds «first» as a key
        // where the other holds «second». Every value is the same, so nothing but
        // the keys can tell them apart.
        var left = Keyed((new object[] { first, "x" }, 0d), (first, 0d), (new object[] { second, "x" }, 0d), ("filler", 0d));
        var right = Keyed((new object[] { second, "x" }, 0d), (new object[] { first, "x" }, 0d), (second, 0d), ("filler", 0d));

        Assert.False(Builtin.Same(left, right));
        Assert.False(Builtin.Same(right, left));

        // And being unequal, they are two keys of an outer lookup rather than one
        // — the duplicate refusal asks this same equality.
        Assert.IsType<Lookup>(Keyed((left, 1d), (right, 2d)));
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

        // A MISS is NOTHING, which is what types «m @ k» as «optional V» — so a
        // forgotten miss is a compile-time error rather than a runtime one, and a
        // «match» stays exhaustive by ordinary typing. Optionals nest, so absent is
        // still told from present-and-nothing: absent is nothing at the outer
        // level.
        Assert.Same(Nothing.Instance, indexing.Apply(Keyed(("a", 1d)), "c"));

        // A LIST index out of range stays an error, and the difference is in kind:
        // a missing key is data with an honest answer, an index past the end is a
        // mistake, and typing every list index «optional» would put an «otherwise»
        // on all of them to pay for a bug.
        Assert.Contains("no position", Assert.IsType<Error>(indexing.Apply(List.Admit(new object[] { 1d }), 5d)).Message);

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
