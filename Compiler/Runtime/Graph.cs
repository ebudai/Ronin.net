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

    /// <summary>When this node's value last actually changed.</summary>
    public long Changed { get; set; }

    /// <summary>Where the clock stood when its body last ran.</summary>
    public long Evaluated { get; set; }

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
        // Declare first. Recording the trigger before Let had a chance to reject
        // the name meant a refused duplicate still replaced the original's body
        // and mode: the declaration threw, and firing the original condition
        // afterwards ran the code that had just been rejected.
        var node = Let(name, trigger);

        whens[name] = new Trigger(body, mode);

        return node;
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

    /// <summary>
    ///     Effect bodies that failed during the last <see cref="Step"/>. A
    ///     <c>let</c> keeps its fault as its value, where the next reader finds
    ///     it; a <c>when</c> has no value and no reader, so its faults collect
    ///     here instead of vanishing.
    /// </summary>
    public IReadOnlyList<Fault> Faults => faults;

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

        Unique(name);

        constants[name] = value;
    }

    public object Read(string name)
    {
        // Before the edge is recorded, because reading a constant creates none.
        if (constants.TryGetValue(name, out var constant)) return constant;

        var read = Reading(name);

        // Arms adoption. A body cannot be stopped mid-flight, so instead the
        // first error it reads is remembered and applied to whatever it returns.
        // A fault arms adoption too: a body that reads one and ignores it would
        // otherwise return a normal value and hide the defect, which is the same
        // hole adoption exists to close.
        if (read is Error failure && adopting.Count is not 0 && adopting[^1].Handling is 0)
        {
            adopting[^1].Error ??= failure;
        }

        return read;
    }

    /// <summary>
    ///     Evaluates without arming adoption, which is what <c>otherwise</c>
    ///     needs: it is the one thing that inspects a failure without inheriting
    ///     it, so the graph must not inherit it on its behalf either.
    /// </summary>
    ///
    /// <remarks>
    ///     Suppression belongs to the frame being evaluated and not to the graph.
    ///     A dirty <c>let</c> recomputed during this read opens a frame of its
    ///     own and must adopt normally inside it — one graph-wide counter
    ///     disarmed that too, so a nested body could read an error, ignore it and
    ///     return a value, and the handler would see the value where a failure had
    ///     passed through. <c>otherwise</c> protects the expression it wraps and
    ///     nothing deeper.
    /// </remarks>
    public T Handling<T>(Func<T> read)
    {
        // Adoption only arms inside a recompute, so «otherwise» outside every
        // body — in a var initialiser, or in a when — has nothing to suppress.
        if (adopting.Count is 0) return read();

        var frame = adopting[^1];

        ++frame.Handling;
        try { return read(); }
        finally { --frame.Handling; }
    }

    private object Reading(string name)
    {
        // an undeclared name is a value like any other failure, not a throw
        if (nodes.TryGetValue(name, out var node) is false) return new Error($"«{name}» is not declared");

        // A pull is demand driven, so a chain of derived values is a chain of
        // stack frames — and a deep enough one took the process out with a
        // StackOverflowException, which cannot be caught and ends the session
        // that always-running exists to protect. A value says so instead, and
        // behaves like every other failure: it adopts, «otherwise» catches it,
        // and the graph carries on.
        if (reading.Count >= Depth)
            return new Error($"«{name}» is more than {Depth} derivations deep. That is past what " +
                             "can be evaluated at once — break the chain with a var, or derive " +
                             "fewer intermediate values.");

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

        // staged while a when body is running, so that a defect part way through
        // it takes every write with it rather than landing half of them
        if (staged is null) pending[name] = value; else staged[name] = value;
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
        faults.Clear();

        // Before any pending write applies and once for the whole turn, so «old
        // x» is the previous step's value consistently — including across every
        // cascade round, which must not see it move. This is where a step
        // boundary falls, and Trigger.Previous has to agree with it.
        foreach (var (source, shadow) in shadows)
        {
            if (Equals(shadow.Value, source.Value)) continue;

            shadow.Value = source.Value;
            shadow.Changed = ++clock;
            MarkDirty(shadow);
        }

        var rounds = 0;

        // At least one round, always. Shadows advance above with no write behind
        // them, so a «when» reading «old x» can be dirtied by nothing but the
        // step itself — and gating the loop on pending writes meant it was
        // dirtied and never examined.
        while ((rounds is 0 || pending.Count is not 0) && rounds < limit)
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
            node.Changed = ++clock;
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

    /// <summary>
    ///     Runs one <c>when</c> body, staging its writes so that a defect inside
    ///     it discards them all.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     A <c>let</c> body's defect became a <see cref="Fault"/> and the session
    ///     survived it; an effect body was called straight and its exception left
    ///     through <see cref="Step"/>, ending the program. Always-running has to
    ///     mean the runtime and not only the pure half of it.
    ///     </para>
    ///     <para>
    ///     Landing the writes queued before the failure would show the graph a
    ///     state no body ever intended — the same hazard that settling before
    ///     firing exists to prevent — and unlike a <c>let</c>, an effect body
    ///     cannot simply be run again, so there is nothing to be recovered by
    ///     keeping them. All or none, and the fault says which body.
    ///     </para>
    /// </remarks>
    private void Fire(string name)
    {
        fired.Add(name);

        staged = [];

        try
        {
            whens[name].Body(this);

            foreach (var (cell, value) in staged) pending[cell] = value;
        }
        catch (Exception defect)
        {
            faults.Add(new Fault($"«{name}» failed and none of its writes were applied: " +
                                 $"{defect.GetType().Name}: {defect.Message}"));
        }
        finally
        {
            staged = null;
        }
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
        Unique(node.Name);

        nodes[node.Name] = node;
        return node;
    }

    /// <summary>
    ///     One name, one declaration — across both stores.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     Replacing a node silently leaves every existing edge pointing at the
    ///     node that was replaced, which is a graph that reads as intact and is
    ///     not. The resolver already rejects a name declared twice; this is the
    ///     same rule one layer down, where it can still be reached directly.
    ///     </para>
    ///     <para>
    ///     Spanning both stores is what a constant not being a node costs. Only
    ///     nodes were checked, and <see cref="Read"/> consults constants first —
    ///     so a constant declared over a var shadowed it outright, with the node
    ///     and all its edges still there and no longer reachable by name.
    ///     Declaring a node over a constant is the same hole from the other side.
    ///     </para>
    /// </remarks>
    private void Unique(string name)
    {
        if (nodes.ContainsKey(name) is false && constants.ContainsKey(name) is false) return;

        throw new InitialisationFailure(
            $"«{name}» is already declared. A second declaration would leave the first " +
            "unreachable by name while its edges still exist — and a constant hides a node " +
            "outright, because a read finds the constant first. Rename one of them.");
    }

    private void Recompute(Node node)
    {
        // CUTOFF. Dirty means "something upstream might have changed", not "did".
        // A coarse value derived from a fine one changes far less often than its
        // source, and without this every intermediate that recomputes to the same
        // value wakes everything below it — the wave runs to the leaves whether
        // or not anything moved.
        if (Settled(node))
        {
            node.Dirty = false;
            return;
        }

        // Clear the old edges first, or a stale dependency keeps the node dirty
        // forever once a conditional switches branches.
        foreach (var dependency in node.Dependencies) nodes[dependency].Dependents.Remove(node.Name);
        node.Dependencies.Clear();

        node.Evaluating = true;
        reading.Add(node);

        adopting.Add(new Adoption());

        object value;
        try
        {
            value = node.Body(this);
        }
        catch (PurityViolation violation)
        {
            value = new Error(violation.Message);
        }
        catch (Exception defect)
        {
            // survivable, because always-running means one bad node must not end
            // the session — but tagged, so it can never pass for a result
            value = new Fault($"{defect.GetType().Name}: {defect.Message}");
        }
        finally
        {
            reading.RemoveAt(reading.Count - 1);
            node.Evaluating = false;
        }

        // ADOPTION. An error read while evaluating wins over whatever the body
        // chose to do with it, so a body that ignores one cannot discard it. The
        // body may still have run: because a let body is pure, running one and
        // throwing the result away has no observable effect, which is what makes
        // this equal to the guarantee the design states.
        var adopted = adopting[^1].Error;
        adopting.RemoveAt(adopting.Count - 1);

        if (adopted is not null && value is not Fault) value = adopted;

        // the clock moves only on a real change, which is what lets a dependent
        // compare its own last run against it
        if (Equals(node.Value, value) is false) node.Changed = ++clock;

        node.Value = value;
        node.Evaluated = clock;
        node.Dirty = false;
        trace.Add(node.Name);
    }

    /// <summary>
    ///     Whether everything this node read is still what it was, so its cached
    ///     value stands and its body need not run.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     Bringing each dependency up to date first is what makes the answer
    ///     trustworthy — a dependency that is itself only dirty has to settle
    ///     before its stamp means anything.
    ///     </para>
    ///     <para>
    ///     Equality here is the language's, which is cheap for a scalar and O(n)
    ///     for an array. When array-valued cells arrive, cutting off on a full
    ///     comparison can cost more than the recompute it saves, and they will
    ///     want a digest or no cutoff at all.
    ///     </para>
    /// </remarks>
    private bool Settled(Node node)
    {
        // never having run is not the same as having read nothing
        if (node.Dependencies.Count is 0) return false;

        foreach (var name in node.Dependencies)
        {
            // Read rather than Recompute: it settles a dirty dependency and
            // still answers for a cycle, and no edge is recorded because this
            // runs before the node is pushed onto the reading stack.
            Read(name);

            if (nodes[name].Changed > node.Evaluated) return false;
        }

        return true;
    }

    /// <summary>
    ///     Pushes the dirty mark through everything downstream.
    /// </summary>
    ///
    /// <remarks>
    ///     Iterative, because this walks a graph whose depth is the program's and
    ///     not the runtime's. Recursing down a long chain of derived values put a
    ///     frame on the stack per link, and a deep enough one ended the process
    ///     with a StackOverflowException — the one failure that cannot be caught
    ///     and so cannot be survived.
    /// </remarks>
    private void MarkDirty(Node node)
    {
        Stack<Node> pending = new();
        pending.Push(node);

        while (pending.Count is not 0)
        {
            foreach (var name in pending.Pop().Dependents)
            {
                var dependent = nodes[name];

                // already marked means its own dependents are too, so descending
                // again is pure rework — and is what bounds this walk
                if (dependent.Dirty) continue;

                dependent.Dirty = true;
                pending.Push(dependent);
            }
        }
    }

    /// <summary>
    ///     One body's adoption state: the failure it has inherited so far, and
    ///     whether <c>otherwise</c> is protecting the read currently running.
    /// </summary>
    ///
    /// <remarks>
    ///     The two live together because they scope together. Keeping the
    ///     suppression on the graph while the adopted error was per body is what
    ///     let one <c>otherwise</c> disarm every nested recompute.
    /// </remarks>
    private sealed class Adoption
    {
        public Error Error { get; set; }

        public int Handling { get; set; }
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

    /// <summary>
    ///     How deep a pull may go before it is a failure rather than a
    ///     computation. Well under what the stack holds, so the message arrives
    ///     instead of the crash.
    /// </summary>
    private const int Depth = 512;

    private readonly int limit = cascades;
    private readonly Dictionary<string, Node> nodes = [];
    private readonly Dictionary<string, object> constants = [];
    private readonly Dictionary<Node, Node> shadows = [];
    private readonly Dictionary<string, Trigger> whens = [];
    private readonly Dictionary<string, object> pending = [];
    private Dictionary<string, object> staged;
    private readonly List<Node> reading = [];
    private readonly List<Adoption> adopting = [];
    private readonly List<string> trace = [];
    private readonly List<string> fired = [];
    private readonly List<Fault> faults = [];
    private long clock;
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
