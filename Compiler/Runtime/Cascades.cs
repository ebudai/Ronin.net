// Copyright © 2026 Eric Budai

using System.Collections.Generic;
using System.Linq;

namespace Ronin.Runtime;

/// <summary>
///     What a <c>when</c> reads and what it writes, which is all a cycle needs.
/// </summary>
///
/// <param name="Feedback">
///     Whether the cycle is the point. Constraint relaxation writes the sizes it
///     reads until they stop moving, and banning that would cost layout solving,
///     physics settling, and every state machine that transitions on its own
///     state. Declared feedback is deliberate, visible and greppable.
/// </param>
internal sealed record Effects(IReadOnlySet<string> Reads, IReadOnlySet<string> Writes, bool Feedback = false);

/// <summary>
///     Finds <c>when</c> cycles before anything runs.
/// </summary>
///
/// <remarks>
///     <para>
///     The first of three tiers, and the only one that catches an accident at
///     the mistake rather than at three in the morning:
///     </para>
///     <list type="number">
///         <item>static — a cycle among declarations, reported here</item>
///         <item>declared — a <c>when</c> that needs feedback says so</item>
///         <item>runtime — <see cref="RunawayCascade"/>, as the backstop</item>
///     </list>
///     <para>
///     Whether a cycle <em>converges</em> is not decidable, which is why the
///     runtime limit stays: this tier cannot tell a settling layout from a
///     runaway thermostat, and a feedback declaration is a promise a programmer
///     can get wrong.
///     </para>
///     <para>
///     No body is analysed. One <c>when</c> precedes another when it writes
///     something the other reads, and a cycle in <em>that</em> graph is a plain
///     graph property.
///     </para>
/// </remarks>
internal static class Cascades
{
    /// <summary>
    ///     Every ring, each given whole and starting from its lowest-sorting
    ///     member so the same ring reads the same way every run.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<string>> Cycles(IReadOnlyDictionary<string, Effects> whens)
    {
        var edges = Precedence(whens);

        List<IReadOnlyList<string>> rings = [];
        HashSet<string> settled = [];
        HashSet<string> walking = [];
        List<string> path = [];

        void Visit(string node)
        {
            walking.Add(node);
            path.Add(node);

            foreach (var next in edges[node].Order())
            {
                if (walking.Contains(next)) rings.Add([.. path[path.IndexOf(next)..], next]);
                else if (settled.Contains(next) is false) Visit(next);
            }

            path.RemoveAt(path.Count - 1);
            walking.Remove(node);
            settled.Add(node);
        }

        foreach (var node in whens.Keys.Order())
        {
            if (settled.Contains(node) is false) Visit(node);
        }

        // a ring every member opted into is the feature, not the bug
        return [.. rings.Where(ring => ring.Any(name => whens[name].Feedback is false))];
    }

    /// <summary>
    ///     The rings as something to show a programmer. Each names its whole ring:
    ///     the three-hop case is unreadable if only one participant is named.
    /// </summary>
    public static IEnumerable<string> Diagnose(IReadOnlyDictionary<string, Effects> whens)
        => Cycles(whens).Select(ring =>
               $"«{string.Join("» → «", ring)}» is a cycle: each writes something the next " +
               "reads, so firing one schedules the next. Stop one of them writing what the " +
               "ring reads, or declare feedback on every when in the ring.");

    private static Dictionary<string, HashSet<string>> Precedence(IReadOnlyDictionary<string, Effects> whens)
    {
        Dictionary<string, HashSet<string>> edges = [];

        foreach (var (name, effects) in whens)
        {
            edges[name] = [.. whens.Where(other => effects.Writes.Overlaps(other.Value.Reads))
                                   .Select(other => other.Key)];
        }

        return edges;
    }
}
