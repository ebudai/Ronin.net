// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using System;
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

/// <summary>A <c>when</c> as declared, and where.</summary>
internal readonly record struct Triggering(string Name, Span Span);

/// <summary>
///     One cell a <c>when</c> writes, and the <c>when</c> it is charged to.
/// </summary>
///
/// <param name="AttributedTo">
///     The <c>when</c> answerable for the write, which is not always the one that
///     performed it: a write reached through a call belongs to the <c>when</c>
///     that made the call, because that is what the programmer can move. Today
///     the two coincide, and the shape is here so that the effect analysis slots
///     in without touching the consumer.
/// </param>
internal sealed record Write(string Cell, string AttributedTo);

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
    ///     Every ring, each given whole and starting from a participant that did
    ///     not declare feedback, so the same ring reads the same way every run and
    ///     the caret lands on a declaration that can actually clear it.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     Legality is a property of the strongly connected COMPONENT rather than
    ///     of the individual rings, and that distinction is a safety rule and not
    ///     a presentation choice. A back-edge walk finds one ring per back edge
    ///     and then settles everything it has been through, so a second ring
    ///     through an already-settled node is never seen: with «a» and «b»
    ///     declaring feedback and «c» not, «a → b → a» is found and allowed, «b»
    ///     settles, and «a → c → b → a» is missed entirely. «c» joins a feedback
    ///     ring without opting into one and nothing complains.
    ///     </para>
    ///     <para>
    ///     Every member of a component lies on a ring with every other member, so
    ///     demanding feedback of all of them is exactly the rule as stated — and
    ///     it costs one linear pass, where enumerating elementary cycles is
    ///     exponential in the worst case for an answer nobody needs.
    ///     </para>
    /// </remarks>
    public static IReadOnlyList<IReadOnlyList<string>> Cycles(IReadOnlyDictionary<string, Effects> whens)
    {
        var edges = Precedence(whens);

        List<IReadOnlyList<string>> rings = [];

        foreach (var component in Components(edges, whens.Keys))
        {
            // One member is a component whether or not it is in a ring, so the
            // self-edge is what separates «writes what it reads» from «writes
            // something nobody reads back».
            if (component.Count is 1 && edges[component[0]].Contains(component[0]) is false) continue;

            // a ring every member opted into is the feature, not the bug
            var offender = component.Where(name => whens[name].Feedback is false)
                                    .Order(StringComparer.Ordinal)
                                    .FirstOrDefault();

            if (offender is null) continue;

            rings.Add(Ring(edges, component, offender));
        }

        return [.. rings.OrderBy(ring => ring[0], StringComparer.Ordinal)];
    }

    /// <summary>
    ///     The shortest ring through <paramref name="from"/>, closed so that it
    ///     reads «a» → «b» → «a».
    /// </summary>
    ///
    /// <remarks>
    ///     A component is a set and a person needs a path, so one ring stands for
    ///     it — the shortest, because the message has to be read, and through the
    ///     participant that did not declare feedback, because that is the one
    ///     declaration the programmer can change to clear it.
    /// </remarks>
    private static IReadOnlyList<string> Ring(Dictionary<string, HashSet<string>> edges,
                                              List<string> component, string from)
    {
        HashSet<string> within = [.. component];
        Dictionary<string, string> came = new() { [from] = null };
        Queue<string> queue = new([from]);
        List<string> closes = [];

        while (queue.Count is not 0)
        {
            var node = queue.Dequeue();

            if (edges[node].Contains(from)) closes.Add(node);

            foreach (var next in edges[node].Where(within.Contains).Order(StringComparer.Ordinal))
            {
                if (came.TryAdd(next, node)) queue.Enqueue(next);
            }
        }

        // Breadth first, so the first to close back is the nearest. Every member
        // of a component reaches every other by definition, so one of them always
        // closes — an empty sequence here would be a defect in the component
        // finder rather than a case to handle, and is left to throw as one.
        List<string> ring = [closes[0]];
        while (came[ring[^1]] is not null) ring.Add(came[ring[^1]]);

        ring.Reverse();
        ring.Add(from);

        return ring;
    }

    /// <summary>
    ///     The strongly connected components, by Tarjan, deterministically
    ///     ordered.
    /// </summary>
    /// <remarks>
    ///     Iterative, because the depth here is the program's rather than this
    ///     algorithm's: a chain of a thousand whens each writing what the next
    ///     reads is a thousand stack frames, and a long enough one ends the
    ///     process with a StackOverflowException — which cannot be caught, so it
    ///     is the one failure a diagnostic pass cannot report.
    ///
    ///     The frame carries the neighbours already walked, which is what
    ///     recursion was keeping in the loop variable, and each node is visited
    ///     twice: once to descend, once to fold the child's low link back.
    /// </remarks>
    private static List<List<string>> Components(Dictionary<string, HashSet<string>> edges,
                                                 IEnumerable<string> nodes)
    {
        Dictionary<string, int> index = [];
        Dictionary<string, int> low = [];
        HashSet<string> stacked = [];
        Stack<string> component = new();
        Stack<(string Node, IEnumerator<string> Neighbours)> walking = new();
        List<List<string>> components = [];
        var counter = 0;

        foreach (var start in nodes.Order(StringComparer.Ordinal))
        {
            if (index.ContainsKey(start)) continue;

            Open(start);

            while (walking.Count is not 0)
            {
                var (node, neighbours) = walking.Peek();

                if (neighbours.MoveNext())
                {
                    var next = neighbours.Current;

                    // an edge into a component already closed says nothing about
                    // this one, which is the case a back-edge walk conflates
                    if (index.ContainsKey(next) is false) Open(next);
                    else if (stacked.Contains(next)) low[node] = Math.Min(low[node], index[next]);

                    continue;
                }

                walking.Pop();

                // fold this node's low link into its parent's, which is what the
                // return from the recursive call used to do
                if (walking.Count is not 0) low[walking.Peek().Node] = Math.Min(low[walking.Peek().Node], low[node]);

                if (low[node] != index[node]) continue;

                List<string> closed = [];
                string member;

                do
                {
                    member = component.Pop();
                    stacked.Remove(member);
                    closed.Add(member);
                }
                while (member != node);

                components.Add(closed);
            }
        }

        return components;

        void Open(string node)
        {
            index[node] = low[node] = counter++;
            component.Push(node);
            stacked.Add(node);
            walking.Push((node, edges[node].Order(StringComparer.Ordinal).GetEnumerator()));
        }
    }

    /// <summary>
    ///     The rings as findings. Each names its whole ring, because the three-hop
    ///     case is unreadable when only one participant is named, and each is
    ///     primary at the participant that did not declare feedback.
    /// </summary>
    public static IEnumerable<Finding> Diagnose(IReadOnlyDictionary<Triggering, Effects> whens)
    {
        var declared = whens.Keys.ToDictionary(when => when.Name, when => when.Span);
        var effects = whens.ToDictionary(when => when.Key.Name, when => when.Value);

        foreach (var ring in Cycles(effects))
        {
            var finding = new CascadeRing(declared[ring[0]], string.Join("» → «", ring));

            // every participant, since the ring is what is wrong and no one of
            // them is more at fault than the others
            // distinct BEFORE skipping: a ring closes on its first member, so
            // skipping one still leaves it in the tail as its own related span
            foreach (var name in ring.Distinct().Skip(1))
            {
                finding.Alongside(declared[name], "also in the ring");
            }

            yield return finding;
        }
    }

    /// <summary>
    ///     Cells written by more than one <c>when</c>, which is a declaration
    ///     error.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     <c>when</c> bodies are unordered relative to each other — they fire in
    ///     one round and nothing says which first — so two of them writing one
    ///     cell has no defined result. Declaration order would make it
    ///     deterministic and silent, which is worse than an error: one write
    ///     lands, the other vanishes, and the program looks fine.
    ///     </para>
    ///     <para>
    ///     Functions are exempt and the difference is the whole justification.
    ///     Two functions writing <c>health</c> is fine because their call sites
    ///     impose an order; two <c>when</c>s have no such thing.
    ///     </para>
    ///     <para>
    ///     Write sets are supplied rather than derived. Deriving them is a shared
    ///     effect analysis with four consumers — this, tier-one cycles, purity,
    ///     and error-ness — all least-fixed-points over one call graph differing
    ///     only in the lattice. Building it inside this file would bury a general
    ///     analysis in one consumer and leave the other three to re-derive it.
    ///     </para>
    /// </remarks>
    public static IEnumerable<Finding> Writers(IReadOnlyDictionary<Triggering, IReadOnlyCollection<Write>> whens)
    {
        Dictionary<string, SortedSet<string>> writers = [];
        Dictionary<string, Span> declared = [];

        foreach (var (when, writes) in whens.OrderBy(entry => entry.Key.Name, StringComparer.Ordinal))
        {
            declared[when.Name] = when.Span;

            foreach (var write in writes)
            {
                if (writers.TryGetValue(write.Cell, out var charged) is false)
                    writers[write.Cell] = charged = new SortedSet<string>(StringComparer.Ordinal);

                charged.Add(write.AttributedTo);
            }
        }

        foreach (var (cell, charged) in writers.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            if (charged.Count < 2) continue;

            var finding = new ManyWriters(declared[charged.First()], cell, charged);

            // named, not merely pointed at: a related span alone reads as
            // «source:4:1: also writes it», which is a place and not a culprit
            foreach (var name in charged.Skip(1)) finding.Alongside(declared[name], $"«{name}» also writes it");

            yield return finding;
        }
    }

    /// <summary>
    ///     Who precedes whom: one <c>when</c> comes before another when it writes
    ///     something the other reads.
    /// </summary>
    ///
    /// <remarks>
    ///     Indexed by cell rather than compared pairwise. Asking every <c>when</c>
    ///     whether its writes overlap every other <c>when</c>'s reads is quadratic
    ///     in the declarations for an answer that is linear in the edges, and at a
    ///     few thousand whens the check cost more than everything it fed.
    /// </remarks>
    private static Dictionary<string, HashSet<string>> Precedence(IReadOnlyDictionary<string, Effects> whens)
    {
        Dictionary<string, List<string>> readers = [];

        foreach (var (name, effects) in whens)
        {
            foreach (var read in effects.Reads)
            {
                if (readers.TryGetValue(read, out var reading) is false) readers[read] = reading = [];

                reading.Add(name);
            }
        }

        Dictionary<string, HashSet<string>> edges = [];

        foreach (var (name, effects) in whens)
        {
            HashSet<string> precedes = [];

            foreach (var write in effects.Writes)
            {
                if (readers.TryGetValue(write, out var reading)) precedes.UnionWith(reading);
            }

            edges[name] = precedes;
        }

        return edges;
    }
}
