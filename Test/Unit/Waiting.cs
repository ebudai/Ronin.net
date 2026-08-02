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
///     live-edit problem at its worst. So n waits become n+1 «when»s and n counts,
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

    [Fact(DisplayName = "and the «when» is gone, not present and disabled")]
    public void AndTheWhenIsGoneNotPresentAndDisabled()
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

    [Fact(DisplayName = "neither «stop» outside a body is a silent no-op")]
    public void NeitherStopOutsideABodyIsASilentNoOp()
    {
        Assert.Throws<InvalidOperationException>(new Graph().Stop);
        Assert.Throws<InvalidOperationException>(new Graph().Return);
    }

    [Theory(DisplayName = "a bound below one is a configuration mistake, refused where it is made")]
    [InlineData(0, 256)]
    [InlineData(-1, 256)]
    [InlineData(64, 0)]
    [InlineData(64, -1)]
    public void ABoundBelowOneIsAConfigurationMistakeRefusedWhereItIsMade(int cascades, int settling)
    {
        // Both count rounds and steps. A cascade limit under one skips the
        // mandatory first round, so a step either does nothing and reports no
        // rounds or throws before applying the write that would have settled it;
        // a settling window under one compares every step while reporting that a
        // count has not fallen in zero of them. Both surfaced far from where the
        // mistake was made and read as defects in something else.
        Assert.Throws<ArgumentOutOfRangeException>(() => new Graph(cascades, settling));
    }

    [Fact(DisplayName = "«Return» records non-advancement, and ends nothing by itself")]
    public void ReturnRecordsNonAdvancementAndEndsNothingByItself()
    {
        // The spec says «return» ends the body where it is written, and it does
        // — by ordinary means, when the lowering emits a «return». This method
        // records only the half the chain needs, so a hand-built body carries on
        // and its later writes apply. Pinned because the two halves are easy to
        // conflate, and the spec now says which is whose.
        var graph = Armed("a", "b");
        graph.Var("after", 0d);
        List<string> ran = [];

        graph.Chain("chain",
                    (scope => scope.Read("a"), _ => ran.Add("x")),
                    (scope => scope.Read("b"), scope =>
                    {
                        scope.Return();
                        scope.Write("after", (double)scope.Read("after") + 1);
                    }),
                    (_ => true, _ => ran.Add("z")));

        graph.Prime();

        Pulse(graph, "a");
        Pulse(graph, "b");

        // the write after it landed
        Assert.Equal(1d, graph.Read("after"));

        // and the run did not advance
        Assert.Equal(["x"], ran);
        Assert.Equal(0d, graph.Read(Graph.Waiting("chain", 2)));
    }

    [Fact(DisplayName = "a chain has segments, and only a chain has counts")]
    public void AChainHasSegmentsAndOnlyAChainHasCounts()
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
        // The count is the whole of the answer: the second segment's condition
        // is «somebody is waiting here, and B», so B on its own reaches nothing.
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
        // the first run, and a program can fix that with its own state. The
        // edge failure cannot be fixed inside the program at all without
        // manufacturing a transition.
        var graph = Armed("a");
        graph.Var("b", true);
        graph.Var("shipped", false);
        List<string> ran = [];

        graph.Chain("when a",
                    (scope => scope.Read("a"), _ => ran.Add("x")),
                    (scope => scope.Read("b"), scope => { ran.Add("y"); scope.Write("shipped", true); }));

        graph.Prime();

        graph.Write("a", true);
        graph.Step();

        Assert.Equal(["x", "y"], ran);

        // Found by audit, and the assertion above could not see it: the quota is
        // built at the start of the step from the chains that had a run in them
        // THEN, and this chain was at rest. Its continuation ran in the same step
        // and looked up a counter that was not in the table, so it faulted AFTER
        // its writes had been published — the bodies ran, the count was right,
        // and the only sign was a fault nobody was reading.
        Assert.Empty(graph.Faults);
        Assert.Equal(true, graph.Read("shipped"));
        Assert.Equal(0d, graph.Read(Graph.Waiting("when a", 1)));
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
        // behind — its write and the arriving count land together, and the guard
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
        // No special case: the second segment's guard is «somebody waiting and
        // B», and it fires while that holds like any other trigger. So B rising
        // with a run waiting takes one, and B rising with none waiting
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

        // a run is waiting, but B is low: the count alone is not the condition
        graph.Write("a", true);
        graph.Step();
        Assert.Equal(["x"], ran);

        // and now B rises, with a run waiting
        graph.Write("b", true);
        graph.Step();
        Assert.Equal(["x", "y"], ran);
    }

    // 11 ------------------------------------------------------------------

    [Fact(DisplayName = "two waits are three «when»s in order, each count returning to zero")]
    public void TwoWaitsAreThreeWhensInOrderEachCountReturningToZero()
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

    [Fact(DisplayName = "«return» ends its own run and leaves the «when» armed")]
    public void ReturnEndsItsOwnRunAndLeavesTheWhenArmed()
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
                     scope => { ran.Add("y"); if (Equals(scope.Read("abandon"), true)) scope.Return(); }),
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

    [Fact(DisplayName = "a chain that never drains is reported as accumulating")]
    public void AChainThatNeverDrainsIsReportedAsAccumulating()
    {
        // A LEAK DETECTOR and not a size limit, because depth does not separate
        // the two: a queue of orders awaiting payment is deep and healthy, and a
        // countdown re-armed faster than it expires is shallow and broken. What
        // separates them is DRAINING — a queue comes back to nothing, an
        // accumulation only rises — so this watches the low-water mark.
        // A short window here, because the window changes how quickly a leak is
        // reported and not whether: a chain that ratchets trips any window
        // eventually, and one that drains trips none. That is why it may be
        // picked, and it is not the round limit's kind of number — that one was
        // load bearing for correctness and killed valid programs when it was
        // wrong.
        Graph graph = new(settling: 8);
        graph.Var("activity", false);
        graph.Var("never", false);

        graph.Chain("when activity",
                    (scope => scope.Read("activity"), _ => { }),
                    (scope => scope.Read("never"), _ => { }));

        graph.Prime();

        var reported = 0;

        for (var step = 0; step < graph.Settling * 3; ++step)
        {
            graph.Write("activity", step % 2 is 0);
            graph.Step();
            reported += graph.Faults.Count;
        }

        // the first window has nothing to compare against; the two after it do
        Assert.Equal(2, reported);
    }

    [Fact(DisplayName = "and one that comes back to nothing is not")]
    public void AndOneThatComesBackToNothingIsNot()
    {
        // The half that makes the detector worth having. This queue is deep and
        // slow, and its low-water mark rises for a long stretch — but it reaches
        // zero inside the window, so it is a queue and not a leak.
        var graph = Armed("go", "done");

        graph.Chain("when go",
                    (scope => scope.Read("go"), _ => { }),
                    (scope => scope.Read("done"), _ => { }));

        graph.Prime();

        var reported = 0;

        for (var cycle = 0; cycle < 6; ++cycle)
        {
            for (var step = 0; step < 40; ++step)
            {
                graph.Write("go", step % 2 is 0);
                graph.Step();
                reported += graph.Faults.Count;
            }

            Pulse(graph, "done");
            reported += graph.Faults.Count;
        }

        Assert.Equal(0, reported);
        Assert.Equal(0d, graph.Read(Graph.Waiting("when go", 1)));
    }

    [Fact(DisplayName = "a queue of any depth drains, however small the limit")]
    public void AQueueOfAnyDepthDrainsHoweverSmallTheLimit()
    {
        // Runs are taken one per round, so draining k of them needs k rounds in
        // one step — and the round limit used to count those, which made it a
        // cap on how deep a chain could ever get. 63 drained and 64 threw, and
        // that was nobody's decision.
        //
        // The limit detects NON-TERMINATION, and draining is the opposite: each
        // run strictly reduces work that already existed. So a round that took
        // one of the runs the step BEGAN with does not count. Runs created
        // during the step are not progress — creating and consuming work inside
        // one settle is the shape the limit is for — which is what the "already
        // pending" qualifier is doing, and why it is not simply "draining
        // rounds do not count".
        Graph graph = new(cascades: 4);
        graph.Var("go", false);
        graph.Var("done", false);

        var ran = 0;

        graph.Chain("when go",
                    (scope => scope.Read("go"), _ => { }),
                    (scope => scope.Read("done"), _ => ++ran));

        graph.Prime();

        for (var each = 0; each < 40; ++each) Pulse(graph, "go");

        Assert.Equal(40d, graph.Read(Graph.Waiting("when go", 1)));

        Pulse(graph, "done");

        Assert.Equal(40, ran);
        Assert.Equal(0d, graph.Read(Graph.Waiting("when go", 1)));
    }

    [Fact(DisplayName = "two positions ready at once do not lose a run between them")]
    public void TwoPositionsReadyAtOnceDoNotLoseARunBetweenThem()
    {
        // Found by audit. Adjacent positions both write the count between them —
        // the earlier adds when it advances, the later takes when it consumes —
        // and both read the same settled front value, so one absolute write
        // replaced the other and a run vanished. The counters both ended at zero,
        // which is what made it a lost run rather than a late one.
        //
        // The fix is that one position of a chain fires per round, which is what
        // "one run per round" meant for the written «when» rather than for each
        // continuation separately.
        var graph = Armed("a", "b", "c");
        List<string> ran = [];

        graph.Chain("chain",
                    (scope => scope.Read("a"), _ => ran.Add("x")),
                    (scope => scope.Read("b"), _ => ran.Add("y")),
                    (scope => scope.Read("c"), _ => ran.Add("z")));

        graph.Prime();

        Pulse(graph, "a");
        Pulse(graph, "b");          // one run parked at wait 2
        Pulse(graph, "a");          // a second parked at wait 1

        // both waits satisfied in the same step
        graph.Write("b", true);
        graph.Write("c", true);
        graph.Step();

        Assert.Equal(["x", "y", "x", "y", "z", "z"], ran);
        Assert.Equal(0d, graph.Read(Graph.Waiting("chain", 1)));
        Assert.Equal(0d, graph.Read(Graph.Waiting("chain", 2)));
    }

    [Fact(DisplayName = "«stop» with positions already deferred leaves nothing behind")]
    public void StopWithPositionsAlreadyDeferredLeavesNothingBehind()
    {
        // Found by audit. A deferred position is queued by name, and «stop»
        // removed the chain's «when»s, its nodes and its counts — but not the
        // names it had queued. The next candidate sort indexes «whens» to order
        // them, so ONE stale name is carried harmlessly and TWO make the
        // comparison itself throw. That is why this needs three segments: a
        // single stale candidate never invokes the comparer and passes.
        var graph = Armed("a", "b", "c");

        graph.Chain("chain",
                    (scope => scope.Read("a"), scope => scope.Stop()),
                    (scope => scope.Read("b"), _ => { }),
                    (scope => scope.Read("c"), _ => { }));

        graph.Prime();

        // two runs in flight, one at each wait
        Pulse(graph, "a");
        Pulse(graph, "b");
        Pulse(graph, "a");

        // the head fires and stops while both continuations are also ready, so
        // both are deferred and then removed underneath the queue
        graph.Write("a", true);
        graph.Write("b", true);
        graph.Write("c", true);
        graph.Step();

        Assert.False(graph.Reacts("chain"));

        // the step that used to throw
        graph.Step();
        graph.Write("a", false);
        graph.Step();
    }

    [Fact(DisplayName = "a deferred position runs in the step that deferred it")]
    public void ADeferredPositionRunsInTheStepThatDeferredIt()
    {
        // Found by audit. Deferring is scheduler work, and the settle loop
        // continued only while WRITES were pending — so when the round that
        // deferred a position wrote nothing, the step ended with the position
        // still owed. A «return» in the head is exactly that: it deliberately
        // writes no next count.
        //
        // The tail then waited for an unrelated step, and in a host that steps
        // only when something changes, for an unrelated event. Both settled
        // rules say otherwise: runs beside the returning one are unaffected, and
        // a satisfied wait proceeds in the same step.
        var graph = Armed("a", "b", "bail");
        List<string> ran = [];

        graph.Chain("chain",
                    (scope => scope.Read("a"),
                     scope => { ran.Add("head"); if (Equals(scope.Read("bail"), true)) scope.Return(); }),
                    (scope => scope.Read("b"), _ => ran.Add("tail")));

        graph.Prime();

        Pulse(graph, "a");          // one run parked at the wait

        ran.Clear();
        graph.Write("bail", true);
        graph.Step();

        // the head fires and returns, and the parked run's wait is satisfied too
        graph.Write("a", true);
        graph.Write("b", true);
        graph.Step();

        Assert.Equal(["head", "tail"], ran);
    }

    [Fact(DisplayName = "a chain at rest costs nothing, however many there are")]
    public void AChainAtRestCostsNothingHoweverManyThereAre()
    {
        // Found by audit, and it was the other half of the sparse-scheduling
        // finding: ordinary «when»s stopped being scanned, and every chain
        // continuation was still visited, sorted and read every round. Worse, the
        // step rebuilt a quota and a low-water reading for every chain there was.
        //
        // A chain at rest has nothing to inherit and nothing to watch. Nothing
        // needs to keep asking it — a run arriving changes its count, and so does
        // taking one, and either wakes it like any other node.
        static long Resting(int chains)
        {
            Graph graph = new();

            for (var each = 0; each < chains; ++each)
            {
                graph.Var($"s{each}", false);
                graph.Var($"t{each}", false);

                var mine = each;

                graph.Chain($"chain {each}",
                            (scope => scope.Read($"s{mine}"), _ => { }),
                            (scope => scope.Read($"t{mine}"), _ => { }));
            }

            graph.Prime();
            graph.Step();

            var before = GC.GetAllocatedBytesForCurrentThread();

            for (var step = 0; step < 50; ++step) graph.Step();

            return (GC.GetAllocatedBytesForCurrentThread() - before) / 50;
        }

        // Flat rather than a number, because the absolute figure is the
        // allocator's business and the shape is the finding: fifty times the
        // chains must not cost fifty times the step.
        var few = Resting(100);
        var many = Resting(5000);

        Assert.True(many <= few * 2, $"{few} bytes at 100 chains, {many} at 5000");
    }

    [Fact(DisplayName = "and one active among many at rest still drains")]
    public void AndOneActiveAmongManyAtRestStillDrains()
    {
        Graph graph = new();

        for (var each = 0; each < 200; ++each)
        {
            graph.Var($"s{each}", false);
            graph.Var($"t{each}", false);

            var mine = each;

            graph.Chain($"chain {each}",
                        (scope => scope.Read($"s{mine}"), _ => { }),
                        (scope => scope.Read($"t{mine}"), _ => { }));
        }

        graph.Prime();

        Pulse(graph, "s7");
        Assert.Equal(1d, graph.Read(Graph.Waiting("chain 7", 1)));

        Pulse(graph, "t7");
        Assert.Equal(0d, graph.Read(Graph.Waiting("chain 7", 1)));
    }

    [Fact(DisplayName = "a run parked at one wait does not excuse work taken at another")]
    public void ARunParkedAtOneWaitDoesNotExcuseWorkTakenAtAnother()
    {
        // Found by audit, and narrower than the cross-chain version below: runs
        // are fungible AT ONE WAIT, so the quota belongs to the counter. Shared
        // across a chain, a run parked at wait 2 paid for work made and taken at
        // wait 1, and the limit stopped firing.
        Graph graph = new(cascades: 2);
        graph.Var("a", false);
        graph.Var("b", false);
        graph.Var("never", false);

        graph.Chain("chain",
                    (scope => scope.Read("a"), _ => { }),
                    (scope => scope.Read("b"), _ => { }),
                    (scope => scope.Read("never"), _ => { }));

        graph.Prime();

        Pulse(graph, "a");
        Pulse(graph, "b");          // parked at wait 2, and it never leaves

        Assert.Equal(1d, graph.Read(Graph.Waiting("chain", 2)));

        graph.Write("a", true);
        graph.Write("b", true);

        Assert.Throws<RunawayCascade>(() => graph.Step());
    }

    [Fact(DisplayName = "a body that fails claims no progress either")]
    public void ABodyThatFailsClaimsNoProgressEither()
    {
        // Found by audit, and the same rule as the «stop» one: a body that fails
        // applies none of its effects. It staged its decrement and claimed the
        // exemption before running, so a body that always throws bought a round
        // for a run still sitting where it was — three of them turned a limit of
        // two into five.
        Graph graph = new(cascades: 2);
        graph.Var("a", false);
        graph.Var("b", false);
        graph.Var("temp", 0d);

        graph.Chain("chain",
                    (scope => scope.Read("a"), _ => { }),
                    (scope => scope.Read("b"), _ => throw new InvalidOperationException("defect")));

        graph.When("spin",
                   scope => scope.Read("temp"),
                   scope => scope.Write("temp", (double)scope.Read("temp") + 1),
                   TriggerMode.Changes);

        graph.Prime();

        for (var each = 0; each < 3; ++each) Pulse(graph, "a");

        graph.Write("b", true);
        graph.Write("temp", 1d);

        Assert.Throws<RunawayCascade>(() => graph.Step());
    }

    [Fact(DisplayName = "a run parked in one chain does not excuse another's new work")]
    public void ARunParkedInOneChainDoesNotExcuseAnothersNewWork()
    {
        // Found by audit. Runs are fungible at ONE WAIT, which is what lets the
        // exemption be a countdown per counter rather than a tag per run — they
        // are fungible neither across the waits of a chain (the test above) nor
        // across chains, and one pooled counter let a run sitting
        // in a chain that never drains excuse every round another chain spent
        // creating and consuming its own work. The limit simply stopped firing.
        Graph graph = new(cascades: 2);
        graph.Var("old head", false);
        graph.Var("never", false);
        graph.Var("new head", false);

        // a chain that will hold one run for ever
        graph.Chain("old chain",
                    (scope => scope.Read("old head"), _ => { }),
                    (scope => scope.Read("never"), _ => { }));

        // and one that makes work and takes it inside a single step
        graph.Chain("new chain",
                    (scope => scope.Read("new head"), _ => { }),
                    (_ => true, _ => { }));

        graph.Prime();

        Pulse(graph, "old head");

        Assert.Equal(1d, graph.Read(Graph.Waiting("old chain", 1)));

        graph.Write("new head", true);

        Assert.Throws<RunawayCascade>(() => graph.Step());
    }

    [Fact(DisplayName = "a body that fails applies none of its effects, «stop» included")]
    public void ABodyThatFailsAppliesNoneOfItsEffectsStopIncluded()
    {
        // Found by audit. A body that fails applies none of its writes — landing
        // the ones queued before the failure would show the graph a state no
        // body intended — and disarming a «when» is an effect like any other. It
        // was applied anyway, so a body that stopped and then threw took the
        // «when» with it while its writes were discarded: half of an intention
        // nobody expressed, and the «when» could never come back.
        Graph graph = new();
        graph.Var("armed", false);

        graph.When("when armed", scope => scope.Read("armed"), scope =>
        {
            scope.Stop();
            throw new InvalidOperationException("defect");
        });

        graph.Prime();
        graph.Write("armed", true);
        graph.Step();

        Assert.True(graph.Reacts("when armed"));
        Assert.Single(graph.Faults);
    }

    [Fact(DisplayName = "and a failed body is tried once more, at the next step and not this one")]
    public void AndAFailedBodyIsTriedOnceMoreAtTheNextStepAndNotThisOne()
    {
        // Found by audit. The run a failed body did not consume is still waiting
        // and nothing it staged applied, so nothing will wake it — only the
        // requeue does. But it was queued in the round's own wake set, which is
        // consumed EVERY round, so "later" meant "again this step if anything
        // else keeps it alive". Unrelated work decided how many times a body
        // ran, and a body that fails after an effect nothing can take back would
        // repeat it before anything about the program had changed.
        var graph = Armed("head", "wait");
        graph.Var("noise", 0d);
        graph.Var("echo", 0d);
        var attempts = 0;

        graph.Chain("chain",
                    (scope => scope.Read("head"), _ => { }),
                    (scope => scope.Read("wait"),
                     _ => { ++attempts; throw new InvalidOperationException("defect"); }));

        // unrelated, finite, and enough to keep the step going for a few rounds
        graph.When("echoing", scope => scope.Read("noise"), scope =>
        {
            if ((double)scope.Read("echo") < 3d) scope.Write("echo", (double)scope.Read("echo") + 1d);
        }, TriggerMode.Changes);

        graph.Prime();
        Pulse(graph, "head");

        attempts = 0;
        graph.Write("wait", true);
        graph.Write("noise", 1d);
        graph.Step();

        Assert.Equal(1, attempts);
        Assert.Single(graph.Faults);

        // and the run is still there, so the next step tries it again
        graph.Step();

        Assert.Equal(2, attempts);
        Assert.Equal(1d, graph.Read(Graph.Waiting("chain", 1)));
    }

    [Fact(DisplayName = "a round that deferred work did not fail to settle")]
    public void ARoundThatDeferredWorkDidNotFailToSettle()
    {
        // Found by audit. Deferring is the scheduler declining to run something
        // already ready, because one position of a chain runs per round — and
        // the round was charged to the author for it. At the boundary that spent
        // the budget before the deferred tail could run, and an inherited tail
        // only shows that taking it is free BY running: the quota learns it
        // afterwards. So the step threw with finite, already-owed work in hand.
        //
        // Two rounds is exactly what this program's own cascade needs: the
        // starter, and the head it arms. Everything after that is draining.
        Graph graph = new(cascades: 2);
        graph.Var("head", false);
        graph.Var("wait", false);
        graph.Var("bail", false);
        graph.Var("starter", false);
        List<string> ran = [];

        graph.Chain("chain",
                    (scope => scope.Read("head"),
                     scope => { ran.Add("head"); if (Equals(scope.Read("bail"), true)) scope.Return(); }),
                    (scope => scope.Read("wait"), _ => ran.Add("tail")));

        graph.When("when starter", scope => scope.Read("starter"), scope =>
        {
            scope.Write("head", true);
            scope.Write("wait", true);
            scope.Write("bail", true);
        });

        graph.Prime();

        // one run parked at the wait, from a step of its own
        graph.Write("head", true);
        graph.Step();
        graph.Write("head", false);
        graph.Step();

        ran.Clear();
        graph.Write("starter", true);
        graph.Step();

        // the head returns, so it writes no next count and the parked run's
        // tail is the only thing left to defer
        Assert.Equal(["head", "tail"], ran);
    }

    [Fact(DisplayName = "and it buys no more of those than it inherited runs")]
    public void AndItBuysNoMoreOfThoseThanItInheritedRuns()
    {
        // The bound, and the reason the exemption is one. A chain whose head
        // keeps being re-armed defers its tail every round, so forgiving every
        // deferral outright would forgive every round and the limit would never
        // fire. Forgiveness is capped at the work that was already here, which
        // is the same principle as the quota: a step may take its time over what
        // it inherited and never over what it makes.
        Graph graph = new(cascades: 4);
        graph.Var("head", false);
        graph.Var("spin", 0d);

        // three positions, so two of them are ready together round after round
        graph.Chain("chain",
                    (scope => scope.Read("head"), _ => { }),
                    (_ => true, _ => { }),
                    (_ => true, _ => { }));

        // re-arms the head every round, for ever
        graph.When("spinning",
                   scope => scope.Read("spin"),
                   scope =>
                   {
                       scope.Write("head", Equals(scope.Read("head"), false));
                       scope.Write("spin", (double)scope.Read("spin") + 1d);
                   },
                   TriggerMode.Changes);

        graph.Prime();
        graph.Write("spin", 1d);

        Assert.Throws<RunawayCascade>(() => graph.Step());
    }

    [Fact(DisplayName = "and work made inside the step still counts against it")]
    public void AndWorkMadeInsideTheStepStillCountsAgainstIt()
    {
        // The half that keeps the limit a limit. A «when» whose body writes what
        // its own trigger reads creates the work for the next round every round,
        // and none of it was pending when the step began — so every round counts
        // and it is caught exactly as before.
        Graph graph = new(cascades: 4);
        graph.Var("temp", 0d);

        graph.When("temp moved",
                   scope => scope.Read("temp"),
                   scope => scope.Write("temp", (double)scope.Read("temp") + 1),
                   TriggerMode.Changes);

        graph.Prime();
        graph.Write("temp", 1d);

        Assert.Throws<RunawayCascade>(() => graph.Step());
    }

    [Fact(DisplayName = "one run per round, because a tail may read what it writes")]
    public void OneRunPerRoundBecauseATailMayReadWhatItWrites()
    {
        // Runs are fungible, so k of them are identical computations and their
        // writes are identical values — collapsing them into a round would be
        // harmless, EXCEPT where the tail reads a cell it writes. Three runs of
        // «shipped = shipped + 1» in one round each read the same front value
        // and each write the same «old + 1», and the count rises by one instead
        // of three.
        //
        // That is the reason to take one per round, and it is sharper than the
        // write-collision argument: batching is safe only for a tail that reads
        // nothing it writes, which is a static property and an optimisation
        // rather than a fix.
        var graph = Armed("go", "done");
        graph.Var("shipped", 0d);

        graph.Chain("when go",
                    (scope => scope.Read("go"), _ => { }),
                    (scope => scope.Read("done"),
                     scope => scope.Write("shipped", (double)scope.Read("shipped") + 1)));

        graph.Prime();

        for (var each = 0; each < 5; ++each) Pulse(graph, "go");

        Pulse(graph, "done");

        Assert.Equal(5d, graph.Read("shipped"));
    }

    [Fact(DisplayName = "a chain accumulates across steps, and the round limit does not see it")]
    public void AChainAccumulatesAcrossStepsAndTheRoundLimitDoesNotSeeIt()
    {
        // One run per round bounds accumulation WITHIN a step: the head fires at
        // most once per round because it is edge-triggered, and the tail takes
        // one, so the count cannot run away inside a settle.
        //
        // Across steps there is no such bound. Each step settles perfectly, so
        // the runaway detector — which counts rounds inside a step — never sees
        // a head that fires once per step and a tail whose condition does not
        // come. That is what the low-water detector above is for; this pins the
        // gap it fills.
        var graph = Armed("activity", "never");

        graph.Chain("when activity",
                    (scope => scope.Read("activity"), _ => { }),
                    (scope => scope.Read("never"), _ => { }));

        graph.Prime();

        for (var pulse = 0; pulse < 100; ++pulse) Pulse(graph, "activity");

        Assert.Equal(100d, graph.Read(Graph.Waiting("when activity", 1)));
    }

    // 17 ------------------------------------------------------------------

    [Fact(DisplayName = "a chain's counts are the runtime's, not the program's")]
    public void AChainsCountsAreTheRuntimesNotThePrograms()
    {
        // Two reasons they cannot be ordinary vars, and both are compile-time
        // analyses: a count is written by the segment that arrives at it AND
        // the one that leaves it, which the writer analysis rejects; and the second
        // «when» reads and writes it, which is a self-loop the cascade checker
        // calls undeclared feedback.
        //
        // Both run over the SOURCE, so a node the frontend never declares is
        // invisible to them. Being a node is what makes a guard dirty when its
        // count moves, which plain state would not.
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

    [Fact(DisplayName = "the two analyses want the segments grouped differently")]
    public void TheTwoAnalysesWantTheSegmentsGroupedDifferently()
    {
        // Anything that unifies these breaks one of them, and the two failures
        // look nothing alike: one is a false diagnostic on a correct program,
        // the other is silence where there should be a diagnostic.
        //
        // Nothing splits a chain from source yet, so this pins the requirement
        // for whoever wires it rather than testing a path that exists.
        Triggering when(string name) => new(name, new SourceText(string.Empty).Span(0, 0));

        // SINGLE-WRITER wants them as ONE writer. The author wrote one «when»,
        // and ownership is a source-level property — the suppression idiom sets
        // a var in one segment and clears it in the next, which is one «when»
        // writing one cell twice.
        Assert.NotEmpty(Cascades.Writers(new Dictionary<Triggering, IReadOnlyCollection<Write>>
        {
            [when("when pressed")] = [new Write("charging", "when pressed")],
            [when("when pressed (after wait 1)")] = [new Write("charging", "when pressed (after wait 1)")],
        }));

        Assert.Empty(Cascades.Writers(new Dictionary<Triggering, IReadOnlyCollection<Write>>
        {
            [when("when pressed")] = [new Write("charging", "when pressed"),
                                      new Write("charging", "when pressed")],
        }));

        // CASCADE wants them DISTINCT, with a real edge between them. Segment 1
        // writing what segment 2 reads IS the chain; merged into one identity it
        // reads as a node that writes what it reads, which is a self-loop.
        Assert.Empty(Cascades.Diagnose(new Dictionary<Triggering, Effects>
        {
            [when("when pressed")] = new(new HashSet<string> { "charging" }, new HashSet<string> { "pressed" }),
            [when("when pressed (after wait 1)")] = new(new HashSet<string> { "fired" },
                                                       new HashSet<string> { "charging" }),
        }));

        Assert.NotEmpty(Cascades.Diagnose(new Dictionary<Triggering, Effects>
        {
            [when("when pressed")] = new(new HashSet<string> { "charging", "fired" },
                                         new HashSet<string> { "pressed", "charging" }),
        }));
    }
}
