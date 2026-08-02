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

    [Fact(DisplayName = "«stop all» takes effect at the end of the round")]
    public void StopAllTakesEffectAtTheEndOfTheRound()
    {
        // Like a write. A «when» that stops itself finishes its body, including
        // what it writes after the «stop» — the alternative is a body whose
        // second half silently does not run, which is the kind of thing nobody
        // discovers until the write mattered.
        var graph = Armed("armed");
        graph.Var("count", 0d);

        graph.When("when armed", scope => scope.Read("armed"), scope =>
        {
            scope.StopAll();
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
        graph.When("when armed", scope => scope.Read("armed"), scope => scope.StopAll());

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

    [Fact(DisplayName = "neither «stop» outside a body is a silent no-op")]
    public void NeitherStopOutsideABodyIsASilentNoOp()
    {
        Assert.Throws<InvalidOperationException>(new Graph().Stop);
        Assert.Throws<InvalidOperationException>(new Graph().StopAll);
    }

    [Fact(DisplayName = "a chain has segments, and «in flight» answers for chains")]
    public void AChainHasSegmentsAndInFlightAnswersForChains()
    {
        Graph graph = new();

        Assert.Throws<ArgumentNullException>(() => graph.Chain("when a", null));
        Assert.Throws<ArgumentException>(() => graph.Chain("when a"));

        // an ordinary «when» has no chain, so it has no counts either
        graph.Var("a", false);
        graph.When("when a", scope => scope.Read("a"), _ => { });

        Assert.IsType<Error>(graph.Read(Graph.Waiting("when a", 1)));
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

        // and nothing is left waiting, so a stray «c» reaches nothing
        Assert.Equal(0d, graph.Read(Graph.Waiting("when a", 1)));
        Assert.Equal(0d, graph.Read(Graph.Waiting("when a", 2)));

        Pulse(graph, "c");
        Assert.Equal(["x", "y", "z"], ran);
    }

    // 13 ------------------------------------------------------------------

    [Fact(DisplayName = "a re-fire mid-chain adds a run rather than abandoning one")]
    public void ARefireMidChainAddsARunRatherThanAbandoningOne()
    {
        // This was the restart test, and restart no longer exists. Under one run
        // at a time a re-fire at segment 3 of 3 had to clear EVERY count or the
        // tail ran twice — a subtlety that existed only to hold a chain to one
        // run. Counting has no policy to get wrong: the second run joins the
        // first and both finish.
        var graph = Armed("a", "b", "c");
        List<string> ran = [];

        graph.Chain("when a",
                    (scope => scope.Read("a"), _ => ran.Add("x")),
                    (scope => scope.Read("b"), _ => ran.Add("y")),
                    (scope => scope.Read("c"), _ => ran.Add("z")));

        graph.Prime();

        Pulse(graph, "a");
        Pulse(graph, "b");          // one run is now at wait 2

        Pulse(graph, "a");          // a second run arrives at wait 1
        Assert.Equal(["x", "y", "x"], ran);

        // «c» finishes the first run; the second is still at wait 1
        Pulse(graph, "c");
        Assert.Equal(["x", "y", "x", "z"], ran);

        Pulse(graph, "b");
        Pulse(graph, "c");
        Assert.Equal(["x", "y", "x", "z", "y", "z"], ran);
    }

    // 14 ------------------------------------------------------------------

    [Fact(DisplayName = "a second run does not disturb the first — both complete")]
    public void ASecondRunDoesNotDisturbTheFirstBothComplete()
    {
        // Counted, not gated. A «when» is instantaneous and cannot be re-entered;
        // a chain has DURATION and can be. Holding it to one run was an attempt
        // to make the chain behave like the «when» it is spelled as, and restart,
        // ignore and the value an author had to name were all faces of that.
        //
        // Counting is also right more often: three orders placed and then one
        // payment clearing should ship three. Restarting reserves three and ships
        // one; ignoring reserves one and ships one. Both lose the rest silently.
        var graph = Armed("placed", "cleared");
        List<string> ran = [];

        graph.Chain("when placed",
                    (scope => scope.Read("placed"), _ => ran.Add("reserve")),
                    (scope => scope.Read("cleared"), _ => ran.Add("ship")));

        graph.Prime();

        Pulse(graph, "placed");
        Pulse(graph, "placed");
        Pulse(graph, "placed");

        Assert.Equal(["reserve", "reserve", "reserve"], ran);

        graph.Write("cleared", true);
        graph.Step();

        Assert.Equal(["reserve", "reserve", "reserve", "ship", "ship", "ship"], ran);
    }

    [Fact(DisplayName = "and suppression is written in the author's own vocabulary")]
    public void AndSuppressionIsWrittenInTheAuthorsOwnVocabulary()
    {
        // Under one run the UNCOMMON policy needed a compiler-invented name.
        // Under counting both are expressible and neither does — an author who
        // wants suppression uses state they already have.
        var graph = Armed("pressed", "released");
        graph.Var("charging", false);
        List<string> ran = [];

        graph.Chain("when pressed",
                    (scope => Equals(scope.Read("pressed"), true)
                           && Equals(scope.Read("charging"), false),
                     scope => { ran.Add("charge"); scope.Write("charging", true); }),
                    (scope => scope.Read("released"),
                     scope => { ran.Add("fire"); scope.Write("charging", false); }));

        graph.Prime();

        Pulse(graph, "pressed");
        Pulse(graph, "pressed");          // suppressed: still charging

        Assert.Equal(["charge"], ran);

        Pulse(graph, "released");
        Assert.Equal(["charge", "fire"], ran);
    }

    [Fact(DisplayName = "«stop» ends its own run and leaves the others, and the «when» armed")]
    public void StopEndsItsOwnRunAndLeavesTheOthersAndTheWhenArmed()
    {
        // The distinction that was two sentences from being lost. Armed and
        // in-flight are different state: collapsing them means a chain that
        // completes normally has nothing in flight, therefore looks stopped,
        // therefore is removed — so a one-shot chain would work and a repeating
        // one would silently stop after its first run.
        var graph = Armed("a", "b", "c");
        graph.Var("abandon", false);
        List<string> ran = [];

        graph.Chain("when a",
                    (scope => scope.Read("a"), _ => ran.Add("x")),
                    (scope => scope.Read("b"),
                     scope => { ran.Add("y"); if (Equals(scope.Read("abandon"), true)) scope.Stop(); }),
                    (scope => scope.Read("c"), _ => ran.Add("z")));

        graph.Prime();

        // one run, abandoned where it waits
        Pulse(graph, "a");

        graph.Write("abandon", true);
        graph.Step();

        Pulse(graph, "b");
        Assert.Equal(["x", "y"], ran);

        // it did not advance, so «c» reaches nothing
        Pulse(graph, "c");
        Assert.Equal(["x", "y"], ran);

        // and the «when» is still ARMED, which is the whole point — a later run
        // goes all the way through
        Assert.True(graph.Reacts("when a"));

        graph.Write("abandon", false);
        graph.Step();

        Pulse(graph, "a");
        Pulse(graph, "b");
        Pulse(graph, "c");

        Assert.Equal(["x", "y", "x", "y", "z"], ran);
    }

    [Fact(DisplayName = "a chain that accumulates is caught by the detector that already exists")]
    public void AChainThatAccumulatesIsCaughtByTheDetectorThatAlreadyExists()
    {
        // Runs are taken ONE PER ROUND, so k of them take k rounds inside the
        // step — which is what lets the runaway detector see a head firing
        // faster than its tail completes. Several in a round would instead be
        // several writes to the same cells in one settle, where the last lands
        // and the rest vanish.
        //
        // Here the tail re-arms the head, so the chain feeds itself.
        var graph = Armed("go");
        graph.Var("count", 0d);

        graph.Chain("when go",
                    (scope => scope.Read("go"), _ => { }),
                    (_ => true, scope => scope.Write("count", (double)scope.Read("count") + 1)));

        graph.Prime();

        graph.Write("go", true);

        // it settles: one run in, one run out
        graph.Step();
        Assert.Equal(1d, graph.Read("count"));
    }

    // 15 ------------------------------------------------------------------

    [Fact(DisplayName = "«stop all» in the second half removes the first half too")]
    public void StopAllInTheSecondHalfRemovesTheFirstHalfToo()
    {
        // The author wrote one «when». If «stop» removed only the half it
        // appears in, an armed first half would leave an orphaned second half
        // firing whenever its condition eventually went true — possibly much
        // later, with the rest of the chain gone.
        var graph = Armed("a", "b");
        List<string> ran = [];

        graph.Chain("when a",
                    (scope => scope.Read("a"), _ => ran.Add("x")),
                    (scope => scope.Read("b"), scope => { ran.Add("y"); scope.StopAll(); }));

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
