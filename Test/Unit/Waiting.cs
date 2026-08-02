// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Runtime;

namespace Unit;

/// <summary>
///     «stop» and «wait until», from <c>docs/handoff/WHENANDWAIT.md</c> §6.
/// </summary>
///
/// <remarks>
///     A «wait until» is compiled away rather than run. It looks like a coroutine
///     feature — a continuation, per-activation state, a re-entrancy policy — and
///     a suspended continuation is live state produced by OLD CODE, which is the
///     live-edit problem at its worst. So n waits become n+1 «when»s and n flags,
///     and there is no continuation anywhere to reload into.
/// </remarks>
[Trait(nameof(Graph), null)]
public class Waiting
{
    /// <summary>A graph with one boolean source, ready to be armed.</summary>
    private static Graph Armed(params string[] sources)
    {
        Graph graph = new();
        foreach (var source in sources) graph.Var(source, false);
        return graph;
    }

    private static void Pulse(Graph graph, string source)
    {
        graph.Write(source, true);
        graph.Step();
        graph.Write(source, false);
        graph.Step();
    }

    // 5 -------------------------------------------------------------------

    [Fact(DisplayName = "«stop» takes effect at the end of the round")]
    public void StopTakesEffectAtTheEndOfTheRound()
    {
        // Like a write. A «when» that stops itself finishes its body, including
        // what it writes after the «stop» — the alternative is a body whose
        // second half silently does not run, which is the kind of thing nobody
        // discovers until the write mattered.
        var graph = Armed("armed");
        graph.Var("count", 0d);

        graph.When("when armed", scope => scope.Read("armed"), scope =>
        {
            scope.Stop();
            scope.Write("count", (double)scope.Read("count") + 1);
        });

        graph.Prime();
        Pulse(graph, "armed");

        Assert.Equal(1d, graph.Read("count"));
    }

    // 6 -------------------------------------------------------------------

    [Fact(DisplayName = "and the «when» is gone, not present and flagged")]
    public void AndTheWhenIsGoneNotPresentAndFlagged()
    {
        // A stopped «when» that lingers still costs an edge walk and still
        // counts toward cascades. "Stopped" that is not gone is the same leak
        // the placement rule exists to prevent.
        var graph = Armed("armed");
        graph.When("when armed", scope => scope.Read("armed"), scope => scope.Stop());

        graph.Prime();
        Pulse(graph, "armed");

        Assert.False(graph.Reacts("when armed"));

        // and its condition is gone with it, so nothing pulls a trigger for a
        // body that no longer exists. An undeclared name is a value like any
        // other failure here rather than a throw.
        Assert.IsType<Error>(graph.Read("when armed"));

        // re-arming does nothing, because there is nothing to arm
        graph.Write("armed", true);
        graph.Step();

        Assert.Empty(graph.Fired);
    }

    [Fact(DisplayName = "«stop» outside a body is a defect, not a silent no-op")]
    public void StopOutsideABodyIsADefectNotASilentNoOp()
        => Assert.Throws<InvalidOperationException>(new Graph().Stop);

    [Fact(DisplayName = "a chain has segments, and «in flight» answers for chains")]
    public void AChainHasSegmentsAndInFlightAnswersForChains()
    {
        Graph graph = new();

        Assert.Throws<ArgumentNullException>(() => graph.Chain("when a", null));
        Assert.Throws<ArgumentException>(() => graph.Chain("when a"));

        // an ordinary «when» has no chain, so nothing declares the value: it is
        // undeclared rather than false, which is a value like any other failure
        graph.Var("a", false);
        graph.When("when a", scope => scope.Read("a"), _ => { });

        Assert.IsType<Error>(graph.Read(Graph.InFlight("when a")));
    }

    // 8 -------------------------------------------------------------------

    [Fact(DisplayName = "one wait: A then B, x then y, once")]
    public void OneWaitAThenBXThenYOnce()
    {
        var graph = Armed("a", "b");
        List<string> ran = [];

        graph.Chain("when a",
                    (scope => scope.Read("a"), _ => ran.Add("x")),
                    (scope => scope.Read("b"), _ => ran.Add("y")));

        graph.Prime();

        Pulse(graph, "a");
        Assert.Equal(["x"], ran);

        Pulse(graph, "b");
        Assert.Equal(["x", "y"], ran);
    }

    // 9 -------------------------------------------------------------------

    [Fact(DisplayName = "B true before A fires does not run the tail early")]
    public void BTrueBeforeAFiresDoesNotRunTheTailEarly()
    {
        // The flag is the whole of the answer: the second segment's condition is
        // «B and the flag», so B on its own reaches nothing.
        var graph = Armed("a", "b");
        List<string> ran = [];

        graph.Chain("when a",
                    (scope => scope.Read("a"), _ => ran.Add("x")),
                    (scope => scope.Read("b"), _ => ran.Add("y")));

        graph.Prime();

        Pulse(graph, "b");
        Assert.Empty(ran);
    }

    // 9a-9d ---------------------------------------------------------------

    [Fact(DisplayName = "a wait whose condition is already true proceeds in the same step")]
    public void AWaitWhoseConditionIsAlreadyTrueProceedsInTheSameStep()
    {
        // LEVEL, not edge. «wait until B» is a guard on a continuation, not a
        // second trigger, and guards are level — «when» is the one
        // edge-triggered construct and it is edge-triggered on its OWN
        // condition.
        //
        // The failure modes are asymmetric and only one is silent. Under edge
        // semantics a prepaid order whose payment already cleared never ships:
        // no error, no diagnostic, and the symptom is "sometimes orders just do
        // not go out". Level semantics can fire too early, but visibly and on
        // the first run, and a program can fix that by clearing the flag. The
        // edge failure cannot be fixed inside the program at all without
        // manufacturing a transition.
        var graph = Armed("a");
        graph.Var("b", true);
        List<string> ran = [];

        graph.Chain("when a",
                    (scope => scope.Read("a"), _ => ran.Add("x")),
                    (scope => scope.Read("b"), _ => ran.Add("y")));

        graph.Prime();

        graph.Write("a", true);
        graph.Step();

        Assert.Equal(["x", "y"], ran);
    }

    [Fact(DisplayName = "and one whose condition is false waits for it")]
    public void AndOneWhoseConditionIsFalseWaitsForIt()
    {
        var graph = Armed("a", "b");
        List<string> ran = [];

        graph.Chain("when a",
                    (scope => scope.Read("a"), _ => ran.Add("x")),
                    (scope => scope.Read("b"), _ => ran.Add("y")));

        graph.Prime();

        graph.Write("a", true);
        graph.Step();
        Assert.Equal(["x"], ran);

        graph.Write("b", true);
        graph.Step();
        Assert.Equal(["x", "y"], ran);
    }

    [Fact(DisplayName = "the guard sees the condition after the segment before it, not before")]
    public void TheGuardSeesTheConditionAfterTheSegmentBeforeItNotBefore()
    {
        // The case that says WHERE level is measured, since both readings are
        // plausible implementations of it: B is true when «a» fires, and the
        // first segment sets it false. The wait sees what the segment left
        // behind — its write and the arming flag land together, and the guard
        // is evaluated after both.
        var graph = Armed("a");
        graph.Var("b", true);
        List<string> ran = [];

        graph.Chain("when a",
                    (scope => scope.Read("a"), scope => { ran.Add("x"); scope.Write("b", false); }),
                    (scope => scope.Read("b"), _ => ran.Add("y")));

        graph.Prime();

        graph.Write("a", true);
        graph.Step();

        Assert.Equal(["x"], ran);

        graph.Write("b", true);
        graph.Step();

        Assert.Equal(["x", "y"], ran);
    }

    [Fact(DisplayName = "«wait until true» is a no-op in the same step")]
    public void WaitUntilTrueIsANoOpInTheSameStep()
    {
        // The degenerate case falls out rather than being special-cased, which
        // is the sign the rule is the right one.
        var graph = Armed("a");
        List<string> ran = [];

        graph.Chain("when a",
                    (scope => scope.Read("a"), _ => ran.Add("x")),
                    (_ => true, _ => ran.Add("y")));

        graph.Prime();

        graph.Write("a", true);
        graph.Step();

        Assert.Equal(["x", "y"], ran);
    }

    // 10 ------------------------------------------------------------------

    [Fact(DisplayName = "a wait's condition going true and false again is an ordinary edge")]
    public void AWaitsConditionGoingTrueAndFalseAgainIsAnOrdinaryEdge()
    {
        // No special case: the second segment's guard is «the flag and B», and
        // it fires when that becomes true like any other trigger. So B rising
        // while the flag is set runs the tail, and B rising while it is not
        // reaches nothing — which is test 9 from the other side.
        var graph = Armed("a", "b");
        List<string> ran = [];

        graph.Chain("when a",
                    (scope => scope.Read("a"), _ => ran.Add("x")),
                    (scope => scope.Read("b"), _ => ran.Add("y")));

        graph.Prime();

        // B rises and falls before the chain is armed at all
        Pulse(graph, "b");
        Assert.Empty(ran);

        // armed, but B is low: the flag alone is not the condition
        graph.Write("a", true);
        graph.Step();
        Assert.Equal(["x"], ran);

        // and now B rises, with the flag set
        graph.Write("b", true);
        graph.Step();
        Assert.Equal(["x", "y"], ran);
    }

    // 11 ------------------------------------------------------------------

    [Fact(DisplayName = "two waits are three «when»s in order, each flag cleared")]
    public void TwoWaitsAreThreeWhensInOrderEachFlagCleared()
    {
        var graph = Armed("a", "b", "c");
        List<string> ran = [];

        graph.Chain("when a",
                    (scope => scope.Read("a"), _ => ran.Add("x")),
                    (scope => scope.Read("b"), _ => ran.Add("y")),
                    (scope => scope.Read("c"), _ => ran.Add("z")));

        graph.Prime();

        Pulse(graph, "a");
        Pulse(graph, "b");
        Pulse(graph, "c");

        Assert.Equal(["x", "y", "z"], ran);

        // and nothing is left armed, so a stray «c» reaches nothing
        Assert.Equal(false, graph.Read(Graph.InFlight("when a")));

        Pulse(graph, "c");
        Assert.Equal(["x", "y", "z"], ran);
    }

    // 13 ------------------------------------------------------------------

    [Fact(DisplayName = "a re-fire at the last segment clears every flag, and the tail runs once")]
    public void ARefireAtTheLastSegmentClearsEveryFlagAndTheTailRunsOnce()
    {
        // The one that catches the partial-clear bug. Setting flag 1 without
        // clearing the others leaves TWO live positions, and the tail then runs
        // for both — a bug whose symptom is a doubled effect long after the
        // re-fire that caused it.
        var graph = Armed("a", "b", "c");
        List<string> ran = [];

        graph.Chain("when a",
                    (scope => scope.Read("a"), _ => ran.Add("x")),
                    (scope => scope.Read("b"), _ => ran.Add("y")),
                    (scope => scope.Read("c"), _ => ran.Add("z")));

        graph.Prime();

        Pulse(graph, "a");
        Pulse(graph, "b");          // now sitting at segment 3 of 3

        Pulse(graph, "a");          // re-fire, mid-flight
        Assert.Equal(["x", "y", "x"], ran);

        Pulse(graph, "c");
        Assert.Equal(["x", "y", "x"], ran);   // «c» reaches nothing: flag 2 was cleared

        Pulse(graph, "b");
        Pulse(graph, "c");
        Assert.Equal(["x", "y", "x", "y", "z"], ran);
    }

    // 14 ------------------------------------------------------------------

    [Fact(DisplayName = "guarding on «in flight» ignores the re-fire instead")]
    public void GuardingOnInFlightIgnoresTheRefireInstead()
    {
        // Restart is the default; ignore is what an author writes, in one
        // clause. Debounce, one-shot and "do not retrigger the animation" are
        // all this.
        var graph = Armed("a", "b");
        List<string> ran = [];

        graph.Chain("when a",
                    (scope => Equals(scope.Read("a"), true)
                           && Equals(scope.Read(Graph.InFlight("when a")), false), _ => ran.Add("x")),
                    (scope => scope.Read("b"), _ => ran.Add("y")));

        graph.Prime();

        Pulse(graph, "a");
        Pulse(graph, "a");          // ignored: the chain is in flight

        Assert.Equal(["x"], ran);

        Pulse(graph, "b");
        Assert.Equal(["x", "y"], ran);
    }

    // 15 ------------------------------------------------------------------

    [Fact(DisplayName = "«stop» in the second half removes the first half too")]
    public void StopInTheSecondHalfRemovesTheFirstHalfToo()
    {
        // The author wrote one «when». If «stop» removed only the half it
        // appears in, an armed first half would leave an orphaned second half
        // firing whenever its condition eventually went true — possibly much
        // later, with the rest of the chain gone.
        var graph = Armed("a", "b");
        List<string> ran = [];

        graph.Chain("when a",
                    (scope => scope.Read("a"), _ => ran.Add("x")),
                    (scope => scope.Read("b"), scope => { ran.Add("y"); scope.Stop(); }));

        graph.Prime();

        Pulse(graph, "a");
        Pulse(graph, "b");

        Assert.Equal(["x", "y"], ran);

        Assert.False(graph.Reacts("when a"));
        Assert.False(graph.Reacts(Graph.Resuming("when a", 1)));

        // and neither half fires again
        Pulse(graph, "a");
        Pulse(graph, "b");

        Assert.Equal(["x", "y"], ran);
    }

    // 17 ------------------------------------------------------------------

    [Fact(DisplayName = "a chain's flags are the runtime's, not the program's")]
    public void AChainsFlagsAreTheRuntimesNotThePrograms()
    {
        // Two reasons they cannot be ordinary vars, and both are compile-time
        // analyses: a flag is written by the segment that sets it AND the one
        // that clears it, which the writer analysis rejects; and the second
        // «when» reads and writes it, which is a self-loop the cascade checker
        // calls undeclared feedback.
        //
        // Both run over the SOURCE, so a node the frontend never declares is
        // invisible to them. Being a node is what makes a guard dirty when its
        // flag moves, which plain state would not.
        Triggering when(string name) => new(name, new SourceText(string.Empty).Span(0, 0));

        var rings = Cascades.Diagnose(new Dictionary<Triggering, Effects>
        {
            [when("when a")] = new(new HashSet<string> { "x" }, new HashSet<string> { "a" }),
            [when("when a after wait 1")] = new(new HashSet<string> { "y" }, new HashSet<string> { "b" }),
        });

        Assert.Empty(rings);

        var writers = Cascades.Writers(new Dictionary<Triggering, IReadOnlyCollection<Write>>
        {
            [when("when a")] = [new Write("x", "when a")],
            [when("when a after wait 1")] = [new Write("y", "when a after wait 1")],
        });

        Assert.Empty(writers);
    }
}
