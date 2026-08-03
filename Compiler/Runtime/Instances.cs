// Copyright © 2026 Eric Budai

using System.Collections.Generic;

namespace Ronin.Runtime;

/// <summary>
///     A stable name for one instance, which an index is not.
/// </summary>
///
/// <remarks>
///     <para>
///     Removal is swap-with-last, so an index is invalidated by the removal of
///     something else entirely. A stored index would then read a different
///     instance and answer confidently — the failure class this project refuses
///     everywhere else, arriving through a data structure rather than a name.
///     </para>
///     <para>
///     The generation is what makes it a name rather than a location. A slot is
///     reused, and the count of how many times it has been reused is not, so a
///     handle held across a removal is recognisably stale and becomes an
///     <see cref="Error"/> instead of an answer.
///     </para>
///     <para>
///     The type travels with it because the arrays are per type: a handle from
///     one population indexes another's members perfectly well, and every one of
///     those reads would be wrong and none of them would say so.
///     </para>
/// </remarks>
internal readonly record struct Instance(string Type, int Slot, int Generation);

/// <summary>
///     Every instance of one type, and the members they share.
/// </summary>
///
/// <remarks>
///     One cell per declared member holding N values, and not one node per
///     instance. Under grouped storage the dependency graph is the size of the
///     SOURCE TEXT; under per-instance nodes it is the size of the world — so
///     edges, dirty propagation, cascade analysis and every diagnostic that
///     names a node scale with how much code was written rather than with how
///     much data exists. That is a comprehensibility property before it is a
///     performance one, and it holds at twelve controls as much as at a hundred
///     thousand entities.
/// </remarks>
internal sealed class Population(string type)
{
    public string Type { get; } = type;

    /// <summary>The member cells, in declaration order.</summary>
    public List<string> Members { get; } = [];

    /// <summary>
    ///     Source member name to cell, which is ownership in one lookup.
    /// </summary>
    ///
    /// <remarks>
    ///     The ordered list is what a column walk needs and the wrong shape for
    ///     the question every read and write asks. Ownership is static after
    ///     declaration, so it is a table rather than a search.
    /// </remarks>
    public Dictionary<string, string> Owns { get; } = [];

    /// <summary>Where a live instance's values sit, or <see cref="Absent"/>.</summary>
    public int this[Instance instance]
        => instance.Type == Type
        && instance.Slot < generations.Count
        && generations[instance.Slot] == instance.Generation
         ? dense[instance.Slot]
         : Absent;

    /// <summary>A slot no longer names anything, or never did.</summary>
    public const int Absent = -1;

    /// <summary>Takes a slot for a new instance, reusing a freed one first.</summary>
    public Instance Take()
    {
        var slot = free.Count is 0 ? Fresh() : free.Pop();

        dense[slot] = owners.Count;
        owners.Add(slot);

        return new Instance(Type, slot, generations[slot]);
    }

    /// <summary>
    ///     Frees an instance's slot and reports which dense index moved into its
    ///     place, so the caller can move the values with it.
    /// </summary>
    ///
    /// <remarks>
    ///     Swap-with-last, so the arrays stay dense and a member is a run rather
    ///     than a run with holes in it. The instance that moved keeps its handle:
    ///     its slot is unchanged and only the index behind it moves, which is the
    ///     whole reason the handle is not the index.
    /// </remarks>
    public (int Removed, int Moved) Release(Instance instance)
    {
        var removed = this[instance];
        var last = owners.Count - 1;

        owners[removed] = owners[last];
        dense[owners[removed]] = removed;
        owners.RemoveAt(last);

        dense[instance.Slot] = Absent;
        ++generations[instance.Slot];
        free.Push(instance.Slot);

        return (removed, last);
    }

    private int Fresh()
    {
        dense.Add(Absent);
        generations.Add(0);

        return dense.Count - 1;
    }

    /// <summary>slot to dense index</summary>
    private readonly List<int> dense = [];

    /// <summary>slot to how many times it has been reused</summary>
    private readonly List<int> generations = [];

    /// <summary>dense index to slot, which is what swap-with-last maintains</summary>
    private readonly List<int> owners = [];

    private readonly Stack<int> free = new();
}
