// Copyright © 2026 Eric Budai

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Ronin.Runtime;

internal enum NodeKind { Var, Let }

/// <summary>
///     When a <c>when</c> fires. Edge triggered in both cases, never level
///     triggered: firing every step while a condition merely holds is almost
///     never wanted and is very hard to notice you have.
/// </summary>
internal enum TriggerMode
{
    /// <summary>On the false to true edge only.</summary>
    BecomesTrue,

    /// <summary>Whenever the value differs from the last settled one.</summary>
    Changes,
}

/// <summary>
///     One <c>var</c>, <c>let</c> or resource in the dependency graph.
/// </summary>
internal sealed class Node
{
    public Node(string name, NodeKind kind, Func<Graph, object> body, object value, bool dirty)
    {
        Name = name;
        Kind = kind;
        Body = body;
        Value = value;
        Dirty = dirty;
    }

    public string Name { get; }

    public NodeKind Kind { get; }

    /// <summary>Null for a <c>var</c>, which has a value rather than a way to get one.</summary>
    public Func<Graph, object> Body { get; }

    /// <summary>What readers see. A <c>var</c> stores it; a <c>let</c> caches it.</summary>
    public object Value { get; set; }

    public bool Dirty { get; set; }

    /// <summary>Set while the body runs, so re-entering it is a detected cycle.</summary>
    public bool Evaluating { get; set; }

    /// <summary>
    ///     What this node read last time it ran. Recorded during evaluation, never
    ///     read off the tree.
    /// </summary>
    public HashSet<string> Dependencies { get; } = [];

    public HashSet<string> Dependents { get; } = [];
}

/// <summary>
///     The reactive graph. Push dirty, pull values.
/// </summary>
///
/// <remarks>
///     <para>
///     Writing a var pushes a dirty mark transitively through its dependents;
///     reading a dirty node pulls a recompute. Nothing recomputes that nobody
///     reads, no topological sort is needed, and glitch freedom falls out — the
///     shared parent of a diamond recomputes exactly once, in dependency order,
///     because the pull reaches it before either child finishes.
///     </para>
///     <para>
///     The rule the rest rests on: <strong>a <c>let</c> body is pure.</strong> It
///     may not assign a var and may not touch a resource. Purity is what makes it
///     safe to re-run a body any number of times, and that is what makes
///     recompute, live editing, replay and eventual parallel evaluation all work.
///     It is enforced here, not assumed.
///     </para>
/// </remarks>
internal sealed class Graph(int cascades = 64)
{
    /// <summary>
    ///     A source. Its initialiser is evaluated once, now, so declaration order
    ///     matters for a <c>var</c> and not for a <c>let</c>.
    /// </summary>
    public Node Var(string name, object value) => Declare(new Node(name, NodeKind.Var, null, value, dirty: false));

    /// <summary>
    ///     A derived node. The body is not evaluated at declaration — it runs on
    ///     first read, and again when a dependency changed and someone asks.
    /// </summary>
    public Node Let(string name, Func<Graph, object> body) => Declare(new Node(name, NodeKind.Let, body, null, dirty: true));

    /// <summary>
    ///     A sink: effectful, produces no value, and pushed rather than pulled.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     A <c>when</c> is a <c>let</c> nobody reads and that is allowed to have
    ///     effects. Nobody reading it is exactly why it cannot be pulled. The
    ///     trigger is an ordinary derived node — it caches, it tracks
    ///     dependencies, it is pulled during settle — and the body hangs off it.
    ///     </para>
    ///     <para>
    ///     A <see cref="TriggerMode.BecomesTrue"/> trigger is a condition and so
    ///     is a boolean, which the frontend is responsible for: a condition that
    ///     is not one is a type error and has no meaning to invent here. The one
    ///     value it can hold that is neither true nor false is a failure, and
    ///     that is handled where it arises rather than guessed at.
    ///     </para>
    /// </remarks>
    public Node When(string name, Func<Graph, object> trigger, Action<Graph> body,
                     TriggerMode mode = TriggerMode.BecomesTrue)
    {
        whens[name] = new Trigger(body, mode);
        return Let(name, trigger);
    }

    /// <summary>
    ///     Establishes each trigger's baseline without firing anything, because a
    ///     condition that is already true at startup has not become true.
    /// </summary>
    public void Prime()
    {
        foreach (var (name, trigger) in whens) trigger.Previous = Read(name);
    }

    public Node this[string name] => nodes[name];

    /// <summary>What fired during the last <see cref="Step"/>, in order.</summary>
    public IReadOnlyList<string> Fired => fired;

    /// <summary>What recomputed since the last <see cref="Step"/> or <see cref="Forget"/>.</summary>
    public IReadOnlyList<string> Trace => trace;

    public void Forget() => trace.Clear();

    public object Read(string name)
    {
        // an undeclared name is a value like any other failure, not a throw
        if (nodes.TryGetValue(name, out var node) is false) return new Error($"«{name}» is not declared");

        // Capture the edge dynamically. A conditional depends on the branch it
        // actually took, and that changes between evaluations — read it off the
        // tree instead and changing an unread branch wakes a node that no longer
        // looks at it.
        if (reading.Count is not 0)
        {
            var reader = reading[^1];
            reader.Dependencies.Add(node.Name);
            node.Dependents.Add(reader.Name);
        }

        if (node.Kind is NodeKind.Var) return node.Value;

        // detected by re-entry, so no static analysis is required
        if (node.Evaluating) return new Error($"cycle through «{node.Name}»");

        if (node.Dirty) Recompute(node);

        return node.Value;
    }

    /// <summary>
    ///     Assignment to a <c>var</c>. Held until the next <see cref="Step"/>, so
    ///     two vars written together are never observed half updated.
    /// </summary>
    public void Write(string name, object value)
    {
        var node = nodes[name];

        if (node.Kind is not NodeKind.Var) throw new PurityViolation($"«{name}» is derived; only its body may set it");

        if (reading.Count is not 0)
            throw new PurityViolation($"«{reading[^1].Name}» is a let and may not assign «{name}»");

        pending[name] = value;
    }

    /// <summary>
    ///     One turn, which is three phases and possibly several rounds of them:
    ///     propagate the writes, settle the graph, then fire what triggered.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     Firing after settling is what stops a <c>when</c> observing a half
    ///     updated graph. A fired body's writes land in the next round's pending
    ///     set and never the current one — otherwise one body's write would be
    ///     visible to a body firing after it in the same round, and the
    ///     consistent-generation guarantee is gone.
    ///     </para>
    ///     <para>
    ///     Returns the number of rounds it took, which is one for a turn that
    ///     fired nothing.
    ///     </para>
    /// </remarks>
    public int Step()
    {
        trace.Clear();
        fired.Clear();

        var rounds = 0;

        while (pending.Count is not 0 && rounds < limit)
        {
            ++rounds;

            Propagate();

            // settle is an ordinary pull, so a trigger reading derived values
            // gets consistent ones
            foreach (var name in Triggered()) Fire(name);
        }

        if (pending.Count is not 0) throw Runaway(rounds);

        return rounds;
    }

    private void Propagate()
    {
        foreach (var (name, value) in pending)
        {
            var node = nodes[name];

            // an equal write wakes nobody, which kills a large fraction of
            // real-world churn for one comparison
            if (Equals(node.Value, value)) continue;

            node.Value = value;
            MarkDirty(node);
        }

        pending.Clear();
    }

    private List<string> Triggered()
    {
        List<string> triggered = [];

        foreach (var (name, trigger) in whens)
        {
            var value = Read(name);

            // a failing trigger is not a firing one, and it still updates the
            // baseline so that recovering does not read as an edge
            if (value is Error)
            {
                trigger.Previous = value;
                continue;
            }

            var previous = trigger.Previous;
            trigger.Previous = value;

            // the first observation establishes a baseline rather than an edge
            if (ReferenceEquals(previous, Unobserved)) continue;

            var fires = trigger.Mode is TriggerMode.Changes
                      ? Equals(value, previous) is false
                      : value is true && previous is not true;

            if (fires) triggered.Add(name);
        }

        return triggered;
    }

    private void Fire(string name)
    {
        fired.Add(name);
        whens[name].Body(this);
    }

    private RunawayCascade Runaway(int rounds)
    {
        var culprits = string.Join(", ", fired.TakeLast(3).Distinct().Select(name => $"«{name}»"));

        return new RunawayCascade(
            $"the graph did not settle after {rounds} rounds; last fired: {culprits}. " +
            "A when body is writing a var its own trigger reads, so every firing schedules " +
            "the next. Stop the body writing once the condition it acts on is satisfied.");
    }

    private Node Declare(Node node)
    {
        nodes[node.Name] = node;
        return node;
    }

    private void Recompute(Node node)
    {
        // Clear the old edges first, or a stale dependency keeps the node dirty
        // forever once a conditional switches branches.
        foreach (var dependency in node.Dependencies) nodes[dependency].Dependents.Remove(node.Name);
        node.Dependencies.Clear();

        node.Evaluating = true;
        reading.Add(node);

        object value;
        try
        {
            value = node.Body(this);
        }
        catch (PurityViolation violation)
        {
            value = new Error(violation.Message);
        }
        finally
        {
            reading.RemoveAt(reading.Count - 1);
            node.Evaluating = false;
        }

        node.Value = value;
        node.Dirty = false;
        trace.Add(node.Name);
    }

    private void MarkDirty(Node node)
    {
        foreach (var name in node.Dependents)
        {
            var dependent = nodes[name];

            // already marked means its own dependents are too, so descending
            // again is pure rework
            if (dependent.Dirty) continue;

            dependent.Dirty = true;
            MarkDirty(dependent);
        }
    }

    /// <summary>Distinguishes "not observed yet" from any value a trigger may hold.</summary>
    private static readonly object Unobserved = new();

    private sealed class Trigger(Action<Graph> body, TriggerMode mode)
    {
        public Action<Graph> Body { get; } = body;
        public TriggerMode Mode { get; } = mode;
        public object Previous { get; set; } = Unobserved;
    }

    private readonly int limit = cascades;
    private readonly Dictionary<string, Node> nodes = [];
    private readonly Dictionary<string, Trigger> whens = [];
    private readonly Dictionary<string, object> pending = [];
    private readonly List<Node> reading = [];
    private readonly List<string> trace = [];
    private readonly List<string> fired = [];
}

/// <summary>
///     A turn that never settled, because firing kept scheduling more firing.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class RunawayCascade(string message) : Exception(message);
