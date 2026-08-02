// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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

    /// <summary>
    ///     Every round the value is true, and not only on the edge.
    /// </summary>
    ///
    /// <remarks>
    ///     What a chain's later segments need. Several runs can be waiting at one
    ///     point and they must be taken ONE PER ROUND: several in a round would
    ///     be several writes to the same cells inside one settle, and the last
    ///     would land while the rest vanished. One per round makes each run its
    ///     own settled round, which is the model working as designed — and it is
    ///     what lets the runaway detector see a chain accumulating, since the
    ///     rounds it already counts are the ones the runs take.
    /// </remarks>
    WhileTrue,
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
internal sealed class Graph(int cascades = 64, int settling = 256)
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
    ///     What «return» in a «when» body compiles to: leave this body, and do
    ///     not advance to the next segment.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     No word of its own, because «return» already means "leave this body
    ///     and do not do the rest" — and since a chain arms its next segment only
    ///     AT a wait, ending the run falls out of leaving the body rather than
    ///     needing a second construct beside it.
    ///     </para>
    ///     <para>
    ///     Runs beside it are unaffected and the «when» stays ARMED, which is
    ///     the distinction that matters. Armed and in-flight are two pieces of
    ///     state, and collapsing them means a chain that completes normally has
    ///     nothing in flight, therefore looks stopped, therefore is removed —
    ///     every time it finishes. A one-shot chain would work and a repeating
    ///     one would silently stop after its first run. An empty count is the
    ///     RESTING STATE of a healthy chain and says nothing about whether the
    ///     «when» should still exist.
    ///     </para>
    /// </remarks>
    public void Return()
    {
        if (firing is null) throw new InvalidOperationException("«return» is only meaningful inside a body");

        stopped = true;
    }

    /// <summary>
    ///     Disarms the «when» whose body is running, for the instance it ran for.
    /// </summary>
    ///
    /// <remarks>
    ///     One word for one thing: «stop» stops the «when». Ending a single run
    ///     is <see cref="Return"/>, which needs no word because «return» already
    ///     has that meaning everywhere else.
    /// </remarks>
    ///
    /// <remarks>
    ///     <para>
    ///     At the END OF THE ROUND, like a write, so a «when» that stops itself
    ///     finishes its body first — including the writes it makes afterwards.
    ///     </para>
    ///     <para>
    ///     And it REMOVES the node rather than disabling it. A stopped «when»
    ///     that lingers still costs an edge walk and still counts toward
    ///     cascades, and "stopped" that is not gone is the same leak the
    ///     placement rule exists to prevent.
    ///     </para>
    ///     <para>
    ///     It can only ever shrink the graph, so a statically legal program stays
    ///     legal: removing nodes can only remove cycles, and <see cref="Cascades"/>
    ///     analyses the never-stops graph. That is why there is no dynamic
    ///     cascade analysis here and why there should not be one.
    ///     </para>
    /// </remarks>
    public void Stop()
    {
        if (firing is null) throw new InvalidOperationException("«stop» is only meaningful inside a «when» body");

        // STAGED, like a write. A body that fails applies none of its effects —
        // landing the writes queued before a failure would show the graph a
        // state no body intended — and disarming a «when» is an effect. Applying
        // it anyway meant a body that stopped and then threw took the «when»
        // with it while its writes were discarded: half of an intention nobody
        // expressed.
        halting = true;
    }

    /// <summary>Whether a «when» by this name is still declared.</summary>
    public bool Reacts(string name) => whens.ContainsKey(name);

    /// <summary>
    ///     A «when» whose body waits, registered as the chain it compiles to.
    /// </summary>
    ///
    /// <param name="segments">
    ///     The body split at each wait. The first segment's condition is the
    ///     «when»'s own; every later one is what its «wait until» named.
    /// </param>
    ///
    /// <remarks>
    ///     <para>
    ///     COMPILED AWAY rather than run. «wait until» looks like a coroutine
    ///     feature — a continuation, per-activation state, a re-entrancy policy —
    ///     and a suspended continuation is live state produced by OLD CODE, which
    ///     is the live-edit problem at its worst: a reload lands mid-body in a
    ///     function whose body has changed. So n waits become n+1 «when»s and n
    ///     flags, and there is no continuation anywhere.
    ///     </para>
    ///     <para>
    ///     RESTART is the default. The first segment clears every flag in the
    ///     chain before setting its own, so a re-fire while the chain is in
    ///     flight abandons it wherever it was. Clearing ALL of them and not
    ///     merely setting the first is what stops a re-fire at segment 3 leaving
    ///     two live positions and running the tail twice. To ignore instead, an
    ///     author guards the first condition with <see cref="InFlight"/>.
    ///     </para>
    ///     <para>
    ///     The flags are nodes HERE and never variables THERE. A flag is written
    ///     by the segment that sets it and the segment that clears it, which the
    ///     writer analysis would reject; and its second «when» reads and writes
    ///     it, which is a self-loop the cascade checker would call undeclared
    ///     feedback. Both of those analyses run over the source, so a node the
    ///     frontend never declares is invisible to them — and being a node is
    ///     what makes a guard dirty when its flag moves, which plain state would
    ///     not.
    ///     </para>
    /// </remarks>
    public void Chain(string name, params (Func<Graph, object> Until, Action<Graph> Body)[] segments)
    {
        ArgumentNullException.ThrowIfNull(segments);

        if (segments.Length is 0) throw new ArgumentException("a chain has at least one segment", nameof(segments));

        var waits = segments.Length - 1;
        var pending = new string[waits];

        for (var wait = 0; wait < waits; ++wait)
        {
            pending[wait] = Waiting(name, wait + 1);
            Var(pending[wait], 0d);
        }

        Split chain = new(pending);
        chains[name] = chain;

        for (var segment = 0; segment < segments.Length; ++segment)
        {
            var (until, body) = segments[segment];

            var reacting = segment is 0 ? name : Resuming(name, segment);
            var arrived = segment is 0 ? null : pending[segment - 1];
            var leaving = segment < waits ? pending[segment] : null;

            chain.Reacting.Add(reacting);

            When(reacting,
                 scope =>
                 {
                     // The first segment's condition is the «when»'s own. Every
                     // later one is "somebody is waiting here, and what the wait
                     // named is true" — the count is what stops «B» on its own
                     // from reaching a tail whose head has not run.
                     if (arrived is null) return until(scope);

                     if (Waiting(scope.Read(arrived)) is 0) return false;

                     // «Equals» and not «is true»: the pattern tests the type and
                     // then the value, and the type test can never fail — a
                     // condition that is not boolean is a type error the frontend
                     // owns rather than something to invent an answer for.
                     return Equals(until(scope), true);
                 },
                 scope =>
                 {
                     // ONE run per firing, which the mode below makes one per
                     // round. «arrived is null» is the first segment, and it is
                     // asked that way rather than by the loop counter: a «for»
                     // variable is ONE variable, so a lambda capturing it reads
                     // the value the loop ended on and every segment believed it
                     // was a later one.
                     if (arrived is not null)
                     {
                         scope.Write(arrived, Waiting(scope.Read(arrived)) - 1);
                         scope.Advanced(name);
                     }

                     body(scope);

                     // «stop» ends THIS run: it simply does not advance. The
                     // «when» stays armed and the runs beside it are untouched,
                     // which is why nothing is cleared here — there is no policy
                     // to apply, because there is no rule holding a chain to one
                     // run at a time.
                     if (leaving is not null && stopped is false)
                         scope.Write(leaving, Waiting(scope.Read(leaving)) + 1);
                 },
                 arrived is null ? TriggerMode.BecomesTrue : TriggerMode.WhileTrue);
        }
    }

    /// <summary>
    /// <summary>
    ///     How many runs are waiting at a point.
    /// </summary>
    ///
    /// <remarks>
    ///     A cast and not a test. These counts are declared and written here and
    ///     nowhere else, so anything but a number is a defect in this file rather
    ///     than a state to tolerate — and a body that throws becomes a fault
    ///     naming itself, which is how it should surface.
    /// </remarks>
    private static double Waiting(object count) => (double)count;

    /// <summary>The count of runs waiting at wait <paramref name="wait"/>.</summary>
    public static string Waiting(string name, int wait) => $"{name} (waiting at {wait})";

    /// <summary>
    ///     The «when» a chain resumes into after wait <paramref name="wait"/>.
    /// </summary>
    ///
    /// <remarks>
    ///     A REPORT and not prose, because nothing generated is typed any more —
    ///     the value an author had to name went with the rule that held a chain
    ///     to one run. These surface in <see cref="Fired"/>, in a fault message
    ///     and in a desugaring view, where a parenthetical says plainly that the
    ///     compiler is talking. Being unspellable, they also cost no protected
    ///     words.
    ///     <para>
    ///     Numbered even at one wait: two waits produce two continuations, and a
    ///     generated name that changes shape when a second is added would be its
    ///     own trap.
    ///     </para>
    /// </remarks>
    public static string Resuming(string name, int wait) => $"{name} (after wait {wait})";

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

        // The finite, known set of runs each chain inherited. Working through
        // them is definite progress toward a fixed point; runs CREATED during
        // the step are not, because creating and consuming work inside one
        // settle is the shape the limit exists to catch. Runs are fungible
        // WITHIN a chain, so the first this many consumed are the ones that were
        // already here — but not across chains, and pooling them let a run
        // sitting in one chain excuse a round spent making and taking work in
        // another.
        foreach (var chain in chains.Values)
            chain.Inherited = chain.Flags.Sum(counter => Waiting(nodes[counter].Value));

        var rounds = 0;
        var counted = 0;

        // At least one round, always. Shadows advance above with no write behind
        // them, so a «when» reading «old x» can be dirtied by nothing but the
        // step itself — and gating the loop on pending writes meant it was
        // dirtied and never examined.
        while ((rounds is 0 || pending.Count is not 0) && counted < limit)
        {
            ++rounds;
            advanced = false;

            Propagate();

            // settle is an ordinary pull, so a trigger reading derived values
            // gets consistent ones
            foreach (var name in Triggered()) Fire(name);

            Stopped();

            // A round that took one of the runs this step began with is the
            // graph settling slowly, not failing to settle: each one strictly
            // reduces work that already existed. The limit detects
            // NON-TERMINATION, and draining terminates — counting it was
            // counting the wrong events, which is why raising the number would
            // only have moved the wall.
            if (advanced is false) ++counted;
        }

        if (pending.Count is not 0) throw Runaway(rounds);

        Draining();

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

            var fires = trigger.Mode switch
            {
                TriggerMode.Changes => Equals(value, previous) is false,
                TriggerMode.WhileTrue => Equals(value, true),
                _ => Equals(value, true) && Equals(previous, true) is false,
            };

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
        firing = name;
        stopped = false;
        halting = false;

        try
        {
            whens[name].Body(this);

            foreach (var (cell, value) in staged) pending[cell] = value;

            if (halting) stopping.Add(name);
        }
        catch (Exception defect)
        {
            faults.Add(new Fault($"«{name}» failed and none of its writes were applied: " +
                                 $"{defect.GetType().Name}: {defect.Message}"));
        }
        finally
        {
            staged = null;
            firing = null;
        }
    }

    /// <summary>
    ///     Applies the round's stops, once every body has run.
    /// </summary>
    ///
    /// <remarks>
    ///     The trigger's own node goes with it. It was declared by
    ///     <see cref="When"/> as an ordinary <c>let</c>, and leaving it behind
    ///     would keep the condition being pulled every settle for a body that no
    ///     longer exists.
    /// </remarks>
    private void Stopped()
    {
        foreach (var name in stopping)
        {
            if (whens.TryGetValue(name, out var trigger) is false) continue;

            trigger.Live.Remove(Alone);

            if (trigger.Live.Count is not 0) continue;

            // The whole chain, wherever in it the stop was written. The author
            // wrote ONE «when»; leaving the other half armed would let it fire
            // whenever its condition eventually went true, possibly much later,
            // with the rest of the chain gone.
            foreach (var member in Belonging(name))
            {
                whens.Remove(member);
                Undeclare(member);
            }
        }

        stopping.Clear();
    }

    /// <summary>
    ///     Every «when» that must go when this one does, and the flags with them.
    /// </summary>
    private IEnumerable<string> Belonging(string name)
    {
        foreach (var (chain, membership) in chains)
        {
            if (membership.Reacting.Contains(name) is false) continue;

            foreach (var counter in membership.Flags) Undeclare(counter);

            chains.Remove(chain);

            return membership.Reacting;
        }

        return [name];
    }

    /// <summary>Removes a node and every edge into it.</summary>
    private void Undeclare(string name)
    {
        // A write queued for it in the same round, which is otherwise applied
        // next round against a node that is gone. The segment that stops a chain
        // clears its own flag first, so this is the ordinary path and not a
        // corner: the write and the removal are both end-of-round effects, and
        // the removal wins.
        pending.Remove(name);

        // No "if it is there". Every caller passes a name it has just found in
        // «whens» or in a chain's flags, and both were declared as nodes when the
        // chain was — so a missing one is a defect here rather than a state to
        // tolerate.
        var node = nodes[name];

        foreach (var dependency in node.Dependencies) nodes[dependency].Dependents.Remove(name);

        foreach (var dependent in node.Dependents) nodes[dependent].Dependencies.Remove(name);

        nodes.Remove(name);
    }

    private RunawayCascade Runaway(int rounds)
    {
        var culprits = string.Join(", ", fired.TakeLast(3).Distinct().Select(name => $"«{name}»"));

        // Every round here MADE the work for the next one, which is now the only
        // way to reach this: a round that took a run the step began with is
        // definite progress and does not count. Draining used to reach it too,
        // and got told a body was writing what its own trigger reads — a
        // confident, specific description of a program the author had not
        // written, which is worse than a vague one because it sends them looking
        // for a bug that is not there.
        return new RunawayCascade(
            $"the graph did not settle after {rounds} rounds; last fired: {culprits}. " +
            "Every round created the work for the next — most often a when body writing a var its own trigger " +
            "reads, so each firing schedules another. Stop the body writing once the condition it acts on is " +
            "satisfied.");
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

        /// <summary>
        ///     Which instances this «when» is still running for.
        /// </summary>
        ///
        /// <remarks>
        ///     A LIVENESS MASK, of which module scope is the one-element case.
        ///     Under one cell per member a type-scope «when» is a single node
        ///     evaluating a predicate across every instance, so «stop» meaning
        ///     "remove the node" would stop the behaviour for all of them — and
        ///     the instance that breaks is not the one whose body ran. Clearing a
        ///     bit and removing the node when the mask empties means the same
        ///     thing in both scopes.
        /// </remarks>
        public HashSet<int> Live { get; } = [Alone];
    }

    /// <summary>The only instance there is, until there are instances.</summary>
    private const int Alone = 0;

    /// <summary>The «when»s and counts one written «when» compiled to.</summary>
    private sealed class Split(IReadOnlyList<string> flags)
    {
        public IReadOnlyList<string> Flags { get; } = flags;
        public List<string> Reacting { get; } = [];

        /// <summary>The fewest runs pending at any point in this window.</summary>
        public double Low { get; set; } = double.MaxValue;

        /// <summary>The same, for the window before it.</summary>
        public double Before { get; set; } = double.MaxValue;

        /// <summary>Steps taken in this window.</summary>
        public int Steps { get; set; }

        /// <summary>How many of this chain's runs the step inherited.</summary>
        public double Inherited { get; set; }
    }

    /// <summary>
    ///     Reports a chain whose pending runs never come back down.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     A LEAK DETECTOR and not a size limit, because the compiler cannot tell
    ///     a leak from a busy queue by looking at the depth: «when order placed {
    ///     reserve; wait until payment cleared; ship }» with ten thousand pending
    ///     is a Tuesday, and «when activity { wait until 5 minutes; save }» with
    ///     ten is a bug. They are structurally identical, so any limit either
    ///     fires on the first or misses the second — and the second is the one
    ///     that grows without end.
    ///     </para>
    ///     <para>
    ///     What separates them is DRAINING. A queue comes back to nothing,
    ///     sometimes slowly; an accumulation only ever rises. So this watches the
    ///     low-water mark rather than the value, and reports when the quietest
    ///     moment of one window is still busier than the quietest moment of the
    ///     last.
    ///     </para>
    ///     <para>
    ///     There is no static tier for this, unlike a cascade ring: a leak and a
    ///     queue are the same shape at compile time, so nothing can be said
    ///     before the program runs. That asymmetry is why the guide's
    ///     chain-versus-deadline rule is load bearing rather than advice.
    ///     </para>
    /// </remarks>
    private void Draining()
    {
        foreach (var (name, chain) in chains)
        {
            var waiting = chain.Flags.Sum(counter => Waiting(nodes[counter].Value));

            if (waiting < chain.Low) chain.Low = waiting;

            if (++chain.Steps < Settling) continue;

            // Strictly increased across the whole window, so the chain did not
            // come back to where it was even once.
            if (chain.Before < chain.Low && chain.Before is not double.MaxValue) faults.Add(Accumulating(name, waiting));

            chain.Before = chain.Low;
            chain.Low = double.MaxValue;
            chain.Steps = 0;
        }
    }

    /// <summary>
    ///     How many steps a chain is watched over before its quietest moment is
    ///     compared with the last.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     PICKED, and allowed to be. It changes how quickly a leak is reported
    ///     and not whether: a chain that ratchets trips any window eventually,
    ///     and one that drains trips none. So there is no principled value to go
    ///     looking for, and adjusting it trades reporting latency against how
    ///     long a slow drain may hold its low-water mark up.
    ///     </para>
    ///     <para>
    ///     Not the round limit's kind of number. That one was load bearing for
    ///     CORRECTNESS — set wrong it killed valid programs, and no tuning fixed
    ///     it because it was counting the wrong events. This one cannot kill
    ///     anything.
    ///     </para>
    /// </remarks>
    public int Settling { get; } = settling;

    private Fault Accumulating(string name, double waiting)
        => new($"«{name}» has {waiting.ToString(CultureInfo.InvariantCulture)} runs pending and the count has not " +
               $"fallen in {Settling.ToString(CultureInfo.InvariantCulture)} steps. A chain gives each trigger its " +
               "own run. If a new one should supersede the pending one instead, a deadline says so: «when activity " +
               "{ save at = now + 5 minutes }» beside «when now >= save at { save }».");

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
    private readonly Dictionary<string, Split> chains = [];
    private readonly Dictionary<string, object> pending = [];
    private readonly HashSet<string> stopping = [];

    /// <summary>The «when» whose body is running, which is what «stop» stops.</summary>
    private string firing;

    /// <summary>Whether that body asked not to advance.</summary>
    private bool stopped;

    /// <summary>Whether it asked to be disarmed, which lands only if it finishes.</summary>
    private bool halting;

    /// <summary>Whether this round took a run the step began with.</summary>
    private bool advanced;

    /// <summary>
    ///     Records that this round consumed one of <paramref name="chain"/>'s
    ///     runs, and whether it was one the step inherited.
    /// </summary>
    ///
    /// <remarks>
    ///     Counted down rather than tagged per run, which is the same answer
    ///     because runs are fungible: the first «Inherited» consumed in a step
    ///     are exactly the ones already pending when it began. Per CHAIN, though
    ///     — fungibility does not cross chains, and one pooled counter let a run
    ///     parked in a chain that never drains excuse every round another chain
    ///     spent creating and consuming its own work.
    /// </remarks>
    private void Advanced(string chain)
    {
        // Indexed, not probed: this is called from a chain's own body, and a
        // chain is removed only with the «when»s that run it — so a missing one
        // is a defect here rather than a state to tolerate.
        var split = chains[chain];

        if (split.Inherited < 1) return;

        --split.Inherited;
        advanced = true;
    }
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
