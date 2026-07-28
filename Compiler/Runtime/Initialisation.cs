// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using System;
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

        // Iterative, because the depth here is the program's: a chain of a
        // thousand constants each reading the one before it is a thousand stack
        // frames, and a long enough one ends the process with a
        // StackOverflowException rather than a diagnostic. The flag on the stack
        // is what a post-order needs — visit the reads, then place the reader.
        Stack<(string Name, bool Placing)> pending = new();

        foreach (var name in initialisers.Keys.OrderDescending(StringComparer.Ordinal))
        {
            pending.Push((name, false));
        }

        while (pending.Count is not 0)
        {
            var (name, placing) = pending.Pop();

            if (placing)
            {
                ordered.Add(name);
                continue;
            }

            if (placed.Add(name) is false) continue;

            pending.Push((name, true));

            // a read of something outside the set is a read of something already
            // there — a literal, a pattern call, a name from an enclosing scope
            foreach (var read in initialisers[name].OrderDescending(StringComparer.Ordinal))
            {
                if (initialisers.ContainsKey(read) && placed.Contains(read) is false) pending.Push((read, false));
            }
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

    /// <summary>The rings as findings, each naming every initialiser in it.</summary>
    public static IEnumerable<Finding> Diagnose(IReadOnlyDictionary<Declared, IReadOnlySet<string>> initialisers)
    {
        var declared = initialisers.Keys.ToDictionary(cell => cell.Name, cell => cell.Span);
        var reads = initialisers.ToDictionary(cell => cell.Key.Name, cell => cell.Value);

        foreach (var ring in Cycles(reads))
        {
            var finding = new Finding(FindingKind.InitialisationRing, declared[ring[0]])
                .Naming("ring", string.Join("» → «", ring));

            // distinct BEFORE skipping: a ring closes on its first member, so
            // skipping one still leaves it in the tail as its own related span
            foreach (var name in ring.Distinct().Skip(1))
            {
                finding.Alongside(declared[name], "also in the ring");
            }

            yield return finding;
        }
    }
}
