// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Ronin.Runtime;

internal enum NodeKind { Var, Let, Shadow }

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
    /// <remarks>
    ///     The cache seeds with <see cref="Nothing"/> rather than nothing at all,
    ///     because a shadow taken before the body has ever run copies that cache,
    ///     and a shadow must seed with nothing and never with a null or an error.
    /// </remarks>
    public Node Let(string name, Func<Graph, object> body)
        => Declare(new Node(name, NodeKind.Let, body, Nothing.Instance, dirty: true));

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

    /// <summary>
    ///     The previous step's value of <paramref name="name"/>, allocated on
    ///     first request.
    /// </summary>
    ///
    /// <remarks>
    ///     Seeded with <see cref="Nothing"/> and never with an error, because an
    ///     error seed latches: the cell errors, so next step its shadow is still
    ///     an error, permanently. The shadow's <c>optional</c> typing then makes a
    ///     missing seed a compile error and <c>otherwise</c> supplies it, which
    ///     needs no new checking.
    /// </remarks>
    public Node Shadow(string name)
    {
        var source = nodes[name];
        var shadowed = SymbolTable.Shadowed + name;

        if (nodes.TryGetValue(shadowed, out var shadow)) return shadow;

        shadow = Declare(new Node(shadowed, NodeKind.Shadow, null, Nothing.Instance, dirty: false));
        shadows[source] = shadow;
        return shadow;
    }

    public Node this[string name] => nodes[name];

    /// <summary>What fired during the last <see cref="Step"/>, in order.</summary>
    public IReadOnlyList<string> Fired => fired;

    /// <summary>What recomputed since the last <see cref="Step"/> or <see cref="Forget"/>.</summary>
    public IReadOnlyList<string> Trace => trace;

    public void Forget() => trace.Clear();

    /// <summary>
    ///     A value evaluated once at initialisation and thereafter
    ///     indistinguishable from a literal.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     A constant is deliberately <em>not</em> a node. It can never change, so
    ///     it can never mark anything dirty, and every edge into one would be an
    ///     edge that can never fire: memory held and marking done for an
    ///     impossible event. Colours, tuning values, layout metrics and string
    ///     tables are numerous and read constantly, so those edges would be most
    ///     of the graph and none of the behaviour.
    ///     </para>
    ///     <para>
    ///     There is no write path to refuse, no dirty set to appear in, and no
    ///     ring to join.
    ///     </para>
    /// </remarks>
    public void Constant(string name, object value)
    {
        // An error here can never clear, because nothing recomputes a constant.
        // It would latch and every reader would inherit it for the life of the
        // program, so this stops the program instead. Same argument that decided
        // a shadow seeds with nothing.
        if (value is Error failure)
            throw new InitialisationFailure(
                $"«{name}» is a constant and its initialiser failed: {failure.Message}. " +
                "A constant is evaluated once, so that error can never clear and every " +
                "reader would inherit it permanently. Fix the initialiser, or make it a " +
                "let so it can recover.");

        constants[name] = value;
    }

    public object Read(string name)
    {
        // Before the edge is recorded, because reading a constant creates none.
        if (constants.TryGetValue(name, out var constant)) return constant;

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

        // a var holds its value and a shadow is handed one; only a let derives it
        if (node.Kind is not NodeKind.Let) return node.Value;

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

        if (node.Kind is NodeKind.Shadow)
            throw new PurityViolation(
                $"«{name}» is the previous value of «{name[SymbolTable.Shadowed.Length..]}» and moves " +
                "only when the step does; write the cell it shadows instead");

        if (node.Kind is NodeKind.Let) throw new PurityViolation($"«{name}» is derived; only its body may set it");

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

        // Before any pending write applies and once for the whole turn, so «old
        // x» is the previous step's value consistently — including across every
        // cascade round, which must not see it move. This is where a step
        // boundary falls, and Trigger.Previous has to agree with it.
        foreach (var (source, shadow) in shadows)
        {
            if (Equals(shadow.Value, source.Value)) continue;

            shadow.Value = source.Value;
            MarkDirty(shadow);
        }

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

    /// <summary>
    ///     A <c>when</c>'s body and the last value its trigger settled at.
    /// </summary>
    ///
    /// <remarks>
    ///     <see cref="Previous"/> is a second implementation of what <c>old</c>
    ///     already means — «when C» fires when <c>C and not old C</c>, and «when y
    ///     changes» fires when <c>y is not old y</c>. Both are now expressible in
    ///     the language, so a rewrite of <c>when</c> should desugar rather than
    ///     keep this field. Until then the two must agree about where a step
    ///     boundary falls, which is asserted by
    ///     <c>Shadows.AChangesTriggerFiresExactlyWhenOldDisagrees</c>; see the
    ///     shadow copy at the top of <see cref="Step"/> for the other half.
    /// </remarks>
    private sealed class Trigger(Action<Graph> body, TriggerMode mode)
    {
        public Action<Graph> Body { get; } = body;
        public TriggerMode Mode { get; } = mode;
        public object Previous { get; set; } = Unobserved;
    }

    private readonly int limit = cascades;
    private readonly Dictionary<string, Node> nodes = [];
    private readonly Dictionary<string, object> constants = [];
    private readonly Dictionary<Node, Node> shadows = [];
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

/// <summary>
///     A program that cannot start, as distinct from one that computed a failure.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class InitialisationFailure(string message) : Exception(message);
