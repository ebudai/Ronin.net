// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace Ronin.Runtime;

internal enum NodeKind { Var, Let, Shadow, Member }

/// <summary>
///     When a <c>when</c> fires. The two an author can write are edge
///     triggered — firing every step while a condition merely holds is almost
///     never wanted and is very hard to notice you have. <see cref="WhileTrue"/>
///     is level triggered and is generated, never written: a wait is satisfied
///     by its condition BEING true, not by its becoming true, so a run arriving
///     at one that already holds proceeds.
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
internal sealed class Graph
{
    /// <summary>
    ///     Both bounds count rounds and steps, so neither is meaningful below
    ///     one.
    /// </summary>
    ///
    /// <remarks>
    ///     A cascade limit under one skips the mandatory first round, so a step
    ///     either does nothing and reports no rounds, or throws before applying
    ///     the write that would have settled it. A settling window under one
    ///     compares every step while reporting that a count has not fallen in
    ///     zero of them. Both are configuration mistakes that surface far from
    ///     where they were made and read as defects in something else.
    /// </remarks>
    public Graph(int cascades = 64, int settling = 256)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(cascades, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(settling, 1);

        limit = cascades;
        Settling = settling;
    }

    /// <summary>
    ///     A source. Its initialiser is evaluated once, now, so declaration order
    ///     matters for a <c>var</c> and not for a <c>let</c>.
    /// </summary>
    public Node Var(string name, object value) => Declare(new Node(name, NodeKind.Var, null, value, dirty: false));

    /// <summary>
    ///     Declares a type: one cell per member, each holding every instance's
    ///     value for it.
    /// </summary>
    ///
    /// <remarks>
    ///     ONE cell and not one node per instance. The graph is then the size of
    ///     the source text rather than the size of the world, so edges, dirty
    ///     propagation, cascade analysis and every diagnostic that names a node
    ///     scale with how much code was written — which is the graph a person
    ///     debugs, at twelve controls as much as at a hundred thousand entities.
    /// </remarks>
    public void Type(string type, params (string Member, object Seed)[] members)
    {
        ArgumentNullException.ThrowIfNull(members);

        if (populations.ContainsKey(type))
            throw new InitialisationFailure($"«{type}» is already declared. Rename one of them.");

        Population population = new(type);

        foreach (var (member, seed) in members)
        {
            Declare(new Node(member, NodeKind.Member, null, new List<object>(), dirty: false));

            population.Members.Add(member);
            seeds[member] = seed;
            belonging[member] = type;
        }

        populations[type] = population;
    }

    /// <summary>A new instance of <paramref name="type"/>, seeded per member.</summary>
    public Instance Create(string type)
    {
        var population = populations[type];
        var instance = population.Take();

        foreach (var member in population.Members) Values(member).Add(seeds[member]);

        return instance;
    }

    /// <summary>
    ///     Removes an instance, moving the last one into its place.
    /// </summary>
    ///
    /// <remarks>
    ///     The instance that moved keeps its handle — only the index behind it
    ///     changes, which is the whole reason a handle is not an index.
    /// </remarks>
    public void Remove(Instance instance)
    {
        if (populations[instance.Type][instance] is Population.Absent)
            throw new InitialisationFailure(
                $"«{instance.Type}» instance {instance.Slot} was already removed. A handle outlives " +
                "the instance it named, which is what stops it naming a different one.");

        // A WRITE, and not a mutation that also remembers to dirty. Removal used
        // to compact the arrays on the spot and advance nothing, so a derived
        // cell that had read the instance stayed cached at its last value — and
        // the stable-handle guarantee held only for whoever read the member
        // directly.
        //
        // Buffering it here is what makes the step order-independent as well: if
        // removal landed the instant it was called, a «when» that removes and one
        // that reads would give two different answers depending on which was
        // declared first, which is the defect buffered writes exist to remove.
        leaving.Add(instance);
    }

    /// <summary>One instance's value for a member.</summary>
    public object Read(string member, Instance instance)
    {
        // Asked of the MEMBER and not only of the handle. Checking that a handle
        // is live in its own population says nothing about whether this member
        // belongs to that population — every type's arrays are indexed the same
        // way, so a live «Box» handle reads a «Crate» member perfectly well and
        // answers with whichever crate happens to sit at that index.
        if (Foreign(member, instance) is Error mismatch) return mismatch;

        var index = populations[instance.Type][instance];

        return index is Population.Absent ? Stale(instance) : ((List<object>)Read(member))[index];
    }

    /// <summary>Writes one instance's value for a member, as of the next round.</summary>
    public void Write(string member, Instance instance, object value)
    {
        if (Foreign(member, instance) is Error mismatch) throw new PurityViolation(mismatch.Message);

        if (populations[instance.Type][instance] is Population.Absent)
            throw new PurityViolation(
                $"«{instance.Type}» instance {instance.Slot} was removed and cannot be written");

        // Staged per INSTANCE and not per index, and not per cell either.
        //
        // Not per cell, because a member is one node holding N values, so two
        // instances written in one step are two writes to the same node and
        // last-write-wins would keep one of them.
        //
        // Not per index, because an index is where an instance sits and removal
        // moves the last one into the hole. A write staged as a location and
        // applied after a removal lands on whoever moved into that slot — the
        // identity failure the generational handle exists to prevent,
        // reintroduced by converting the handle to a location before the write
        // settled.
        if (arriving.TryGetValue(member, out var writes) is false) arriving[member] = writes = [];

        writes[instance] = value;
    }

    private Error Foreign(string member, Instance instance)
        => belonging.TryGetValue(member, out var type) && type == instance.Type
         ? null
         : new Error($"«{member}» is not a member of «{instance.Type}»");

    private static Error Stale(Instance instance)
        => new($"this handle named an instance of «{instance.Type}» that has been removed. " +
               "Its slot belongs to a different instance now, which is why the handle is refused " +
               "rather than answered.");

    private List<object> Values(string member) => (List<object>)nodes[member].Value;

    private readonly Dictionary<string, Population> populations = [];
    private readonly Dictionary<string, object> seeds = [];

    /// <summary>Which type each member belongs to, which a handle cannot say.</summary>
    private readonly Dictionary<string, string> belonging = [];
    private readonly Dictionary<string, Dictionary<Instance, object>> arriving = [];

    /// <summary>Instances removed this round, applied where every write is.</summary>
    private readonly HashSet<Instance> leaving = [];

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

        whens[name] = new Trigger(body, mode) { Order = whens.Count };

        woken.Add(name);

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
        var shadowed = Injection.Shadow.Of(name);

        if (nodes.TryGetValue(shadowed, out var shadow)) return shadow;

        shadow = Declare(new Node(shadowed, NodeKind.Shadow, null, Nothing.Instance, dirty: false));
        shadows[source] = shadow;
        return shadow;
    }

    public Node this[string name] => nodes[name];

    /// <summary>
    ///     How many nodes there are, which instances must not change.
    /// </summary>
    ///
    /// <remarks>
    ///     Exposed for the test that pins the binding decision. It is the whole
    ///     of what "the graph is the size of the source text" means, and a
    ///     comment saying so would not survive an optimisation pass.
    /// </remarks>
    public int Declared => nodes.Count;

    /// <summary>What fired during the last <see cref="Step"/>, in order.</summary>
    public IReadOnlyList<string> Fired => fired;

    /// <summary>
    ///     Half of what «return» in a «when» body compiles to: do not advance to
    ///     the next segment.
    /// </summary>
    ///
    /// <remarks>
    ///     HALF, and deliberately. Leaving the body is the lowering's job and
    ///     happens by ordinary means — a «return» in the source returns — so this
    ///     records only the part the chain needs to know. Calling it by hand does
    ///     not end anything: the statements after it run, and their writes apply.
    /// </remarks>
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

        returned = true;
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
    ///     COUNTS, and there is no continuation anywhere.
    ///     </para>
    ///     <para>
    ///     COUNTED, not held to one. A «when» is instantaneous — it fires on an
    ///     edge and its body runs to completion in one step — and a chain has
    ///     DURATION, so it can be re-entered. A second trigger while a run is
    ///     pending starts a second run and both finish; there is no restart, no
    ///     ignore, and nothing an author has to name. Suppression, where it is
    ///     wanted, is written with state the author already has.
    ///     </para>
    ///     <para>
    ///     One position of a chain fires per round. Adjacent positions both write
    ///     the count between them, and both read the same settled front value, so
    ///     two firing together lost a run to last-write-wins — and the segments
    ///     are deliberately one writer to the single-writer rule, so they could
    ///     collide on an author's cell the same way.
    ///     </para>
    ///     <para>
    ///     The counts are nodes HERE and never variables THERE. A count is
    ///     written by the segment that arrives at it and the one that leaves it,
    ///     which the writer analysis would reject; and the second «when» reads
    ///     and writes it, which is a self-loop the cascade checker would call
    ///     undeclared feedback. Both of those analyses run over the source, so a
    ///     node the frontend never declares is invisible to them — and being a
    ///     node is what makes a guard dirty when its count moves, which plain
    ///     state would not.
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
            membership[reacting] = name;

            if (arrived is not null) consumes[reacting] = arrived;

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
                         scope.Consuming(arrived);
                     }

                     body(scope);

                     // «return» ends THIS run: it simply does not advance.
                     // The «when» stays armed and the runs beside it are
                     // untouched, which is why nothing is cleared here — there
                     // is no policy to apply, because there is no rule holding a
                     // chain to one run at a time.
                     //
                     // «stop» is the other word and does not come through here:
                     // it takes the whole «when» away, and the runs beside it
                     // with it.
                     if (leaving is not null && returned is false)
                     {
                         scope.Write(leaving, Waiting(scope.Read(leaving)) + 1);
                         scope.active.Add(name);
                     }
                 },
                 arrived is null ? TriggerMode.BecomesTrue : TriggerMode.WhileTrue);
        }
    }

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
        // settle is the shape the limit exists to catch.
        //
        // Per COUNTER, because that is the whole extent of the fungibility: runs
        // are interchangeable AT ONE WAIT, so the first this many consumed there
        // are the ones that were already here. A run parked at wait 2 is not
        // interchangeable with one taken at wait 1, and a quota shared across a
        // chain let the parked one pay for work newly made and consumed
        // somewhere else in it. Sharing one across CHAINS was worse again.
        // One attempt each, at a step boundary, which is the earliest point at
        // which anything about the program can have changed.
        foreach (var name in stalled) woken.Add(name);

        stalled.Clear();

        credits.Clear();

        // Only chains with a run in them. A chain at rest has nothing to inherit
        // and nothing to watch, and walking every one made a no-op step cost
        // O(chains) — the same shape as scanning every «when», one level down,
        // and it is what the allocation measurement was actually finding.
        foreach (var name in active)
        {
            var chain = chains[name];

            // ONE record per wait, holding both credits. They were two tables
            // read from the same number, and the second outlived what it was a
            // reading OF: consuming the last run parked here left its unspent
            // displacement credit behind to forgive a run created later at the
            // same wait. Kept together, the two cannot come apart.
            foreach (var counter in chain.Counts) credits[counter] = new Credit(Waiting(nodes[counter].Value));
        }

        var rounds = 0;
        var counted = 0;


        // At least one round, always. Shadows advance above with no write behind
        // them, so a «when» reading «old x» can be dirtied by nothing but the
        // step itself — and gating the loop on pending writes meant it was
        // dirtied and never examined.
        // Deferred positions are work this step still owes. Settling on pending
        // writes alone let a ready continuation fall out of the step entirely
        // when the round that deferred it wrote nothing — a «return» in the head
        // writes no next count — so the tail waited for an unrelated step, and in
        // an event-driven host possibly for an unrelated event.
        while ((rounds is 0
             || pending.Count is not 0
             || arriving.Count is not 0
             || leaving.Count is not 0
             || deferred.Count is not 0) && counted < limit)
        {
            ++rounds;
            servicing.Clear();

            Propagate();

            // settle is an ordinary pull, so a trigger reading derived values
            // gets consistent ones
            var ran = Triggered();

            foreach (var name in ran) Fire(name);

            Stopped();
            Displaced();

            // A round that took one of the runs this step began with is the
            // graph settling slowly, not failing to settle: each one strictly
            // reduces work that already existed. The limit detects
            // NON-TERMINATION, and draining terminates — counting it was
            // counting the wrong events, which is why raising the number would
            // only have moved the wall.
            //
            // Nor did a round that DEFERRED work fail to settle. It declined to
            // run something already ready, because one position of a chain runs
            // per round — the scheduler's own throttle, and charging the author
            // for it spent the budget before an inherited tail could show that
            // taking it would have been free. Which it only shows by running:
            // that is why the round has to happen at all.
            //
            // Bounded, because a spinning chain defers too, and bounded BY THE
            // POSITION: what forgives a round is a run that was parked at the
            // very wait the round declined to serve. A graph-wide total let a
            // queue draining healthily in one chain pay for work another chain
            // made and deferred this step — the cross-chain subsidy the quota
            // was made per counter to stop, arriving by the other door.
            //
            // And EVERY firing in the round, not any one of them. Several
            // independent reactions share a round, so reducing an owned credit
            // to "this round was free" handed the exemption to whatever else
            // happened to fire beside it — a queue draining three deep bought
            // three extra rounds for an unrelated «when» writing what its own
            // trigger reads. The credit is spent on the work it belongs to and
            // covers that; a round is free when there was nothing else in it.
            if (ran.Count is 0 || ran.Exists(name => servicing.Contains(name) is false)) ++counted;
        }

        // Deferred work outstanding is non-settlement exactly as a pending write
        // is: the step ran out of rounds with something still to do.
        if (pending.Count is not 0 || arriving.Count is not 0 || leaving.Count is not 0 || deferred.Count is not 0)
            throw Runaway(rounds, counted);

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

        foreach (var (member, writes) in arriving)
        {
            var values = Values(member);
            var moved = false;

            foreach (var (instance, value) in writes)
            {
                // Resolved HERE and not when the write was made, which is the
                // whole point of staging the handle: removal is applied below,
                // so an instance leaving this round is still where it was and
                // the write lands on it rather than on whoever moves into its
                // slot. Compaction then discards both together.
                var index = populations[instance.Type][instance];

                if (Equals(values[index], value)) continue;

                values[index] = value;
                moved = true;
            }

            if (moved is false) continue;

            nodes[member].Changed = ++clock;
            MarkDirty(nodes[member]);
        }

        arriving.Clear();

        // Removal, through the same door. Compacting the arrays without
        // advancing anything left a derived reader cached at the value it last
        // saw, so the stable handle held for a direct read and not for one
        // through a «let» — confidently wrong, and for ever if nothing else
        // happened to dirty it.
        foreach (var instance in leaving)
        {
            var population = populations[instance.Type];
            var (removed, moved) = population.Release(instance);

            foreach (var member in population.Members)
            {
                var values = Values(member);

                values[removed] = values[moved];
                values.RemoveAt(moved);

                // The whole column, which is what a member write already does.
                // A per-row dirty set would be finer and does not exist yet;
                // until it does this is correct and coarse rather than wrong.
                nodes[member].Changed = ++clock;
                MarkDirty(nodes[member]);
            }
        }

        leaving.Clear();
    }

    private List<string> Triggered()
    {
        List<string> triggered = [];

        // The «when»s that could have moved, not every one there is. A trigger
        // whose node was not dirtied has the value its baseline already holds,
        // so asking it can only confirm that — and asking all of them made a
        // step cost O(whens) however few changed, which is exactly backwards for
        // the sparse updates this runtime is for.
        //
        // Sorted back into declaration order, because which «when» fires first
        // must not depend on which happened to be dirtied.
        // Filtered BEFORE the sort, not during the walk below. The comparison
        // indexes «whens», so one stale name is carried harmlessly and two make
        // the sort itself throw — and a chain that «stop» removed leaves exactly
        // that behind, because its deferred positions were queued before it went.
        List<string> candidates = [.. woken.Concat(deferred).Distinct().Where(whens.ContainsKey)];

        woken.Clear();
        deferred.Clear();

        candidates.Sort((left, right) => whens[left].Order.CompareTo(whens[right].Order));

        // At most one position of a chain per round. Two adjacent positions both
        // write the count between them — the earlier adds when it advances, the
        // later takes when it consumes — and both read the same settled front
        // value, so one absolute write replaced the other and a run was lost.
        // The same holds for any two positions and an author's cell, because the
        // segments are deliberately ONE writer to the single-writer rule.
        //
        // One per round for the whole written «when», not for each continuation
        // separately, which is what "one run per round" meant all along.
        claimed.Clear();

        foreach (var name in candidates)
        {
            var trigger = whens[name];

            var value = Read(name);

            // a failing trigger is not a firing one, and it still updates the
            // baseline so that recovering does not read as an edge
            if (value is Error)
            {
                trigger.Previous = value;
                continue;
            }

            var previous = trigger.Previous;

            // the first observation establishes a baseline rather than an edge
            var fires = ReferenceEquals(previous, Unobserved) is false && trigger.Mode switch
            {
                TriggerMode.Changes => Equals(value, previous) is false,
                TriggerMode.WhileTrue => Equals(value, true),
                _ => Equals(value, true) && Equals(previous, true) is false,
            };

            // Deferred, and the baseline is left alone so the edge survives to
            // the next round. Advancing it would consume the transition and the
            // position would simply never run — so it stays a candidate too.
            if (fires && membership.TryGetValue(name, out var chain) && claimed.TryAdd(chain, name) is false)
            {
                deferred.Add(name);
                continue;
            }

            trigger.Previous = value;

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
    ///     firing exists to prevent — so there is nothing to be recovered by
    ///     keeping them. All or none, and the fault says which body.
    ///     </para>
    ///     <para>
    ///     Unlike a <c>let</c>, an effect body cannot be re-run at will: it is
    ///     offered exactly one more attempt, at the next step, because the run it
    ///     did not consume is still waiting and nothing else will wake it. That
    ///     is a retry and not a loop — see <c>stalled</c>.
    ///     </para>
    /// </remarks>
    private void Fire(string name)
    {
        fired.Add(name);

        staged = [];
        firing = name;
        returned = false;
        halting = false;
        consuming = null;

        try
        {
            whens[name].Body(this);

            // Every book closed before anything is published. The fault below
            // says none of its writes were applied, and that has to stay true of
            // a defect ANYWHERE in the firing, not only of one inside the body:
            // publishing first left a path that applied all of them and said it
            // had applied none.
            Consumed();

            if (halting) stopping.Add(name);

            foreach (var (cell, value) in staged) pending[cell] = value;
        }
        catch (Exception defect)
        {
            // Nothing it staged applied, so nothing it would have changed will
            // wake it: a run it did not consume is still waiting, and only this
            // puts the position back in front of the scheduler.
            //
            // NEXT step, though, and not this round. «woken» is consumed every
            // round, so a retry put there ran again as many times as unrelated
            // work happened to keep the step alive — the count decided by
            // something with nothing to do with it. A body that failed after an
            // effect nothing can take back would repeat that effect before
            // anything about the program had changed.
            stalled.Add(name);

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
                woken.Remove(member);
                deferred.Remove(member);
                stalled.Remove(member);
                consumes.Remove(member);
                Undeclare(member);
            }
        }

        stopping.Clear();
    }

    /// <summary>
    ///     Every «when» that must go when this one does, and the counts with them.
    /// </summary>
    private IEnumerable<string> Belonging(string name)
    {
        if (membership.TryGetValue(name, out var chain) is false) return [name];

        var split = chains[chain];

        foreach (var counter in split.Counts) Undeclare(counter);

        foreach (var reacting in split.Reacting) membership.Remove(reacting);

        active.Remove(chain);
        chains.Remove(chain);

        return split.Reacting;
    }

    /// <summary>Removes a node and every edge into it.</summary>
    private void Undeclare(string name)
    {
        // A write queued for it in the same round, which is otherwise applied
        // next round against a node that is gone. The segment that stops a chain
        // takes from its own count first, so this is the ordinary path and not a
        // corner: the write and the removal are both end-of-round effects, and
        // the removal wins.
        pending.Remove(name);

        // No "if it is there". Every caller passes a name it has just found in
        // «whens» or in a chain's counts, and both were declared as nodes when the
        // chain was — so a missing one is a defect here rather than a state to
        // tolerate.
        var node = nodes[name];

        foreach (var dependency in node.Dependencies) nodes[dependency].Dependents.Remove(name);

        foreach (var dependent in node.Dependents) nodes[dependent].Dependencies.Remove(name);

        nodes.Remove(name);
    }

    private RunawayCascade Runaway(int rounds, int counted)
    {
        var culprits = string.Join(", ", fired.TakeLast(3).Distinct().Select(name => $"«{name}»"));

        // Both numbers, because they stopped being the same one. What is bounded
        // is rounds spent on work the step MADE; rounds spent draining what it
        // inherited, or displaced by the head that owns it, are free and are not
        // in this count. Counted and not created, because the last round of a
        // step fires nothing and is charged — it makes nothing either, so
        // naming the count after created work would be a second small lie in
        // the same sentence. Reporting the physical figure and then saying every one
        // of those rounds created the next was a confident, specific description
        // of a program the author had not written — which is worse than a vague
        // one, because it sends them looking for a bug that is not there.
        //
        // «Likely» for the same reason. These are the last three that fired, not
        // three the runtime has any evidence against.
        return new RunawayCascade(
            $"the graph did not settle: {counted} rounds counted against the limit, out of {rounds} in " +
            "all. " +
            $"Likely sources, being the last to fire: {culprits}. Most often a when body writes a var its own " +
            "trigger reads, so each firing schedules another. Stop the body writing once the condition it acts " +
            "on is satisfied.");
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

                if (whens.ContainsKey(name)) woken.Add(name);

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

        /// <summary>
        ///     Where this «when» was declared, which is the order they fire in.
        /// </summary>
        ///
        /// <remarks>
        ///     Kept because the round no longer walks every «when» in table
        ///     order: it visits the ones that could have changed, and they have
        ///     to be put back into the order a reader wrote them.
        /// </remarks>
        public int Order { get; init; }
    }

    /// <summary>The only instance there is, until there are instances.</summary>
    private const int Alone = 0;

    /// <summary>The «when»s and counts one written «when» compiled to.</summary>
    private sealed class Split(IReadOnlyList<string> counters)
    {
        /// <summary>One count per wait, in order.</summary>
        ///
        /// <remarks>
        ///     Counts, and once flags. A chain held one run at a time and each
        ///     wait was set or clear; runs are counted now, and a name saying
        ///     otherwise is how the superseded model keeps being read back out of
        ///     the code.
        /// </remarks>
        public IReadOnlyList<string> Counts { get; } = counters;
        public List<string> Reacting { get; } = [];

        /// <summary>The fewest runs pending at any point in this window.</summary>
        public double Low { get; set; } = double.MaxValue;

        /// <summary>The same, for the window before it.</summary>
        public double Before { get; set; } = double.MaxValue;

        /// <summary>Steps taken in this window.</summary>
        public int Steps { get; set; }


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
        // A copy, because a chain that has emptied leaves «active» below.
        foreach (var name in active.ToArray())
        {
            var chain = chains[name];

            var waiting = 0d;

            foreach (var counter in chain.Counts) waiting += Waiting(nodes[counter].Value);

            // At rest: nothing to watch, and its quietest moment is zero, so the
            // window begins again from there when a run next arrives.
            if (waiting is 0)
            {
                active.Remove(name);
                chain.Before = 0;
                chain.Low = double.MaxValue;
                chain.Steps = 0;
                continue;
            }

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
    public int Settling { get; }

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

    private readonly int limit;
    private readonly Dictionary<string, Node> nodes = [];
    private readonly Dictionary<string, object> constants = [];
    private readonly Dictionary<Node, Node> shadows = [];
    private readonly Dictionary<string, Trigger> whens = [];
    private readonly Dictionary<string, Split> chains = [];

    /// <summary>Which chain each generated «when» belongs to.</summary>
    private readonly Dictionary<string, string> membership = [];

    /// <summary>How many runs each wait had when the step began.</summary>
    private readonly Dictionary<string, Credit> credits = [];

    /// <summary>The counter each continuation takes from, fixed when it is written.</summary>
    private readonly Dictionary<string, string> consumes = [];

    /// <summary>
    ///     What the runs parked at one wait may be forgiven, by kind.
    /// </summary>
    ///
    /// <remarks>
    ///     Two credits per run and not one, which was measured: spending one on
    ///     whichever came first cost exactly what having no exemption at all
    ///     cost, because «k» runs make «k» displacements and «k» drains against a
    ///     supply of «k». They are separate because they are separate events —
    ///     and they are here together because neither may outlive the run.
    /// </remarks>
    private sealed class Credit(double parked)
    {
        /// <summary>Runs parked here when the step began, still to be taken.</summary>
        public double Drains { get; set; } = parked;

        /// <summary>Rounds those runs may still forgive for being displaced.</summary>
        public double Displacements { get; set; } = parked;
    }

    /// <summary>«when»s whose condition may have moved since the last round.</summary>
    private readonly HashSet<string> woken = [];

    /// <summary>Positions whose body failed, to be tried once at the next step.</summary>
    private readonly HashSet<string> stalled = [];

    /// <summary>Positions this round deferred, which the step still owes.</summary>
    private readonly HashSet<string> deferred = [];

    /// <summary>Chains with a run in them, the only ones with anything to do.</summary>
    private readonly HashSet<string> active = [];
    private readonly Dictionary<string, object> pending = [];
    private readonly HashSet<string> stopping = [];

    /// <summary>The «when» whose body is running, which is what «stop» stops.</summary>
    private string firing;

    /// <summary>Whether the body called <see cref="Return"/>, which is per firing.</summary>
    ///
    /// <remarks>
    ///     Named for the word that sets it. It was «stopped», and the comment
    ///     where it is read said «stop» ends this run — the one misdescription
    ///     that has already sent a design round and an audit round the wrong way.
    /// </remarks>
    private bool returned;

    /// <summary>Whether it asked to be disarmed, which lands only if it finishes.</summary>
    private bool halting;

    /// <summary>The counter it took a run from, spent only if it finishes.</summary>
    private string consuming;

    /// <summary>What fired this round in service of work the step inherited.</summary>
    private readonly HashSet<string> servicing = [];

    /// <summary>The position each chain ran this round, which is what displaces.</summary>
    private readonly Dictionary<string, string> claimed = [];

    /// <summary>
    ///     Records that this round consumed one of <paramref name="chain"/>'s
    ///     runs, and whether it was one the step inherited.
    /// </summary>
    ///
    /// <remarks>
    ///     Counted down rather than tagged per run, which is the same answer
    ///     because runs are fungible AT ONE WAIT: the first quota consumed there
    ///     is exactly a run that was already waiting there when the step began.
    ///     They are not fungible across waits or across chains, and a quota
    ///     shared any wider let a run parked in one place pay for work made and
    ///     taken in another.
    ///     <para>
    ///     Recorded rather than spent, because a body that fails applies none of
    ///     its effects: the decrement it staged is discarded, so claiming the
    ///     progress would buy an exemption for a run still sitting there.
    ///     <see cref="Fire"/> spends it once the body has finished.
    ///     </para>
    /// </remarks>
    private void Consuming(string counter) => consuming = counter;

    /// <summary>
    ///     Whether this round declined to serve a run that was already here.
    /// </summary>
    ///
    /// <remarks>
    ///     One run forgives one round, at its own wait. A position absent from
    ///     the table belongs to a chain that was at rest when the step began, so
    ///     everything waiting there arrived during it and none of it is
    ///     inherited; a position present with nothing left has already been
    ///     forgiven for every run it was holding.
    /// </remarks>
    private void Displaced()
    {
        foreach (var name in deferred)
        {
            // «consumes», not a per-step copy of the association: which counter a
            // continuation takes from is fixed when the chain is written, and a
            // second table rebuilt each step to say the same thing is how the
            // two readings drifted apart in the first place.
            if (credits.TryGetValue(consumes[name], out var credit) is false || credit.Displacements < 1) continue;

            --credit.Displacements;

            // The head that owns it, which is the whole of what the run pays
            // for. It was deferred because that position claimed the chain
            // first, so that position is the firing being forgiven.
            servicing.Add(claimed[membership[name]]);
        }
    }

    /// <summary>Spends the quota a finished body claimed, if it had one.</summary>
    ///
    /// <remarks>
    ///     Probed, and not indexed. The table is built at the start of the step
    ///     from the chains that had a run in them THEN, so a chain woken during
    ///     the step is legitimately absent — and its first wait may be satisfied
    ///     already, putting a continuation here in the same step. Absent means
    ///     nothing inherited, which is the truth about it: a run this step made
    ///     and this step took is exactly the work the limit is counting, so it
    ///     buys no exemption. Indexing called that state a defect and faulted the
    ///     ordinary «wait until true».
    /// </remarks>
    private void Consumed()
    {
        if (consuming is null) return;

        if (credits.TryGetValue(consuming, out var credit) && credit.Drains >= 1)
        {
            --credit.Drains;
            servicing.Add(firing);

            // Displacement credit is a reading of what is parked here, so it
            // cannot outlast it. Runs at one wait are fungible: after a drain the
            // most that can still be displaced is what is still standing there,
            // and anything above that belonged to a run that has already gone.
            credit.Displacements = Math.Min(credit.Displacements, credit.Drains);
        }

        consuming = null;
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
