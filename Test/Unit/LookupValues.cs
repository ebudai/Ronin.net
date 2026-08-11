// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Runtime;

namespace Unit;

/// <summary>
///     The runtime lookup value: admitted on the same boundary as a list,
///     insertion-ordered, unordered for equality, keys canonicalised at
///     construction, one depth measure across both kinds.
/// </summary>
[Trait(nameof(Lookup), null)]
public class LookupValues
{
    private static KeyValuePair<object, object>[] Pairs(params (object Key, object Value)[] entries)
        => [.. entries.Select(entry => new KeyValuePair<object, object>(entry.Key, entry.Value))];

    private static object Keyed(params (object Key, object Value)[] entries) => List.Admit(Pairs(entries));

    [Fact(DisplayName = "a pair-carrier is admitted as a lookup, in insertion order")]
    public void APairCarrierIsAdmittedAsALookupInInsertionOrder()
    {
        var lookup = Assert.IsType<Lookup>(Keyed(("a", 1d), ("b", 2d)));

        Assert.Equal(2, lookup.Count);
        Assert.Equal("a", lookup[0].Key);
        Assert.Equal("b", lookup[1].Key);
        Assert.Equal(2d, lookup[1].Value);
        Assert.Equal("[a = 1, b = 2]", lookup.ToString());
        Assert.Equal(["a", "b"], lookup.Select(entry => entry.Key));
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
