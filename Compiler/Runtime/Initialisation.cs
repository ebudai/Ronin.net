// Copyright © 2026 Eric Budai

using System.Collections.Generic;
using System.Linq;

namespace Ronin.Runtime;

/// <summary>
///     The order initialisers run in, and the cycles that would prevent one.
/// </summary>
///
/// <remarks>
///     <para>
///     Constants and vars go in <em>one</em> graph rather than constants among
///     themselves. Ordering constants alone never places
///     «constant initial health = health;» at all, because <c>health</c> is a
///     var and so is not in that graph — and that snapshot is precisely the
///     static-initialisation-order trap every language with this feature has
///     fallen into. With both kinds in one set the order is defined, so the
///     snapshot needs no warning: it is simply well-placed.
///     </para>
///     <para>
///     A cycle across the mixed set is an error, found by the same detector
///     written for <c>when</c> rings — same shape, different node set.
///     </para>
/// </remarks>
internal static class Initialisation
{
    /// <summary>
    ///     Evaluation order for a set of initialisers, given what each one reads.
    ///     False when a cycle makes an order impossible, in which case
    ///     <see cref="Cycles"/> says which.
    /// </summary>
    public static bool TryOrder(IReadOnlyDictionary<string, IReadOnlySet<string>> initialisers,
                                out IReadOnlyList<string> order)
    {
        if (Cycles(initialisers).Count is not 0)
        {
            order = [];
            return false;
        }

        List<string> ordered = [];
        HashSet<string> placed = [];

        void Place(string name)
        {
            placed.Add(name);

            // a read of something outside the set is a read of something already
            // there — a literal, a pattern call, a name from an enclosing scope
            foreach (var read in initialisers[name].Order())
            {
                if (initialisers.ContainsKey(read) && placed.Contains(read) is false) Place(read);
            }

            ordered.Add(name);
        }

        foreach (var name in initialisers.Keys.Order())
        {
            if (placed.Contains(name) is false) Place(name);
        }

        order = ordered;
        return true;
    }

    /// <summary>
    ///     The rings among initialisers. One writes its own name and reads the
    ///     names its initialiser mentions, which is the shape
    ///     <see cref="Cascades"/> already searches.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<string>> Cycles(
        IReadOnlyDictionary<string, IReadOnlySet<string>> initialisers)
        => Cascades.Cycles(initialisers.ToDictionary(
               entry => entry.Key,
               entry => new Effects(entry.Value, new HashSet<string> { entry.Key })));

    public static IEnumerable<string> Diagnose(IReadOnlyDictionary<string, IReadOnlySet<string>> initialisers)
        => Cycles(initialisers).Select(ring =>
               $"«{string.Join("» → «", ring)}» is a cycle: each initialiser reads the one " +
               "before it, so none of them can be evaluated first. Break the ring by giving " +
               "one of them a value that does not depend on the others.");
}
