// Copyright © 2026 Eric Budai

using Ronin.Runtime;

namespace Unit;

/// <summary>
///     The scenarios from <c>docs/handoff/event_scenarios.py</c>. A <c>when</c> is
///     a third node kind: a sink that is effectful, produces no value, and is
///     pushed after the graph settles because nobody reads it and so nothing can
///     pull it.
/// </summary>
[Trait(nameof(Graph), null)]
public class Events
{
    private static readonly Func<object, object, object> Add
        = Builtin.Lift((left, right) => (double)left + (double)right);

    private static readonly Func<object, object, object> Multiply
        = Builtin.Lift((left, right) => (double)left * (double)right);

    private static readonly Func<object, object, object> Exceeds
        = Builtin.Lift((left, right) => (double)left > (double)right);

    [Fact(DisplayName = "a rejected when leaves the graph as it found it")]
    public void ARejectedWhenLeavesTheGraphAsItFoundIt()
    {
        // The trigger was recorded before Let had a chance to reject the name, so
        // a duplicate declaration threw AND replaced the original's body and
        // mode. The declaration looked refused and the graph had already taken
        // it: firing the original condition ran the code that had just been
        // rejected.
        Graph graph = new();
        graph.Var("armed", false);
        List<string> ran = [];

        graph.When("on armed", scope => scope.Read("armed"), _ => ran.Add("original"));

        Assert.Throws<InitialisationFailure>(
            () => graph.When("on armed", scope => scope.Read("armed"), _ => ran.Add("rejected"),
                             TriggerMode.Changes));

        graph.Prime();
        graph.Write("armed", true);
        graph.Step();

        Assert.Equal(["original"], ran);
    }

    // 1 -------------------------------------------------------------------

    [Fact(DisplayName = "a condition fires on the edge, not while it holds")]
    public void AConditionFiresOnTheEdgeNotWhileItHolds()
    {
        Graph graph = new();
        graph.Var("x", 0d);
        graph.Var("alarms", 0d);
        graph.When("x is high",
                   scope => Exceeds(scope.Read("x"), 6d),
                   scope => scope.Write("alarms", Add(scope.Read("alarms"), 1d)));
        graph.Prime();

        foreach (var value in new[] { 7d, 8d, 9d })
        {
            graph.Write("x", value);
            graph.Step();
        }

        Assert.Equal(1d, graph.Read("alarms"));

        // dropping below and crossing again is a new edge
        graph.Write("x", 2d);
        graph.Step();
        graph.Write("x", 10d);
        graph.Step();

        Assert.Equal(2d, graph.Read("alarms"));
    }

    // 2 -------------------------------------------------------------------

    [Fact(DisplayName = "a changes trigger fires on every distinct value")]
    public void AChangesTriggerFiresOnEveryDistinctValue()
    {
        Graph graph = new();
        graph.Var("y", 1d);
        graph.Var("log", 0d);
        graph.When("y moved",
                   scope => scope.Read("y"),
                   scope => scope.Write("log", Add(scope.Read("log"), 1d)),
                   TriggerMode.Changes);
        graph.Prime();

        foreach (var value in new[] { 2d, 2d, 3d, 3d, 4d })
        {
            graph.Write("y", value);
            graph.Step();
        }

        Assert.Equal(3d, graph.Read("log"));
    }

    // 3 -------------------------------------------------------------------

    [Fact(DisplayName = "nothing fires just because the program started")]
    public void NothingFiresJustBecauseTheProgramStarted()
    {
        Graph graph = new();
        graph.Var("hp", 0d);
        graph.Var("deaths", 0d);
        graph.When("is dead",
                   scope => (double)scope.Read("hp") <= 0d,
                   scope => scope.Write("deaths", Add(scope.Read("deaths"), 1d)));

        graph.Prime();

        // already true at startup, which is not having become true
        Assert.Equal(0d, graph.Read("deaths"));
        Assert.Empty(graph.Fired);
    }

    // 4 -------------------------------------------------------------------

    [Fact(DisplayName = "a when sees a settled graph, never a half updated one")]
    public void AWhenSeesASettledGraph()
    {
        Graph graph = new();
        graph.Var("width", 1d);
        graph.Var("height", 1d);
        graph.Let("area", scope => Multiply(scope.Read("width"), scope.Read("height")));

        List<(object Width, object Height, object Area)> observed = [];
        graph.When("area is big",
                   scope => Exceeds(scope.Read("area"), 50d),
                   scope => observed.Add((scope.Read("width"), scope.Read("height"), scope.Read("area"))));
        graph.Prime();

        graph.Write("width", 10d);
        graph.Write("height", 10d);
        graph.Step();

        Assert.Equal([(10d, 10d, 100d)], observed);
    }

    // 5 -------------------------------------------------------------------

    [Fact(DisplayName = "a fired body's writes land in the next round")]
    public void AFiredBodysWritesLandInTheNextRound()
    {
        Graph graph = new();
        graph.Var("trigger", 0d);
        graph.Var("a", 0d);
        graph.Var("b", 0d);

        List<(string Who, object Saw)> order = [];

        graph.When("first", scope => scope.Read("trigger"),
                   scope =>
                   {
                       order.Add(("first sees b", scope.Read("b")));
                       scope.Write("a", 1d);
                   },
                   TriggerMode.Changes);

        graph.When("second", scope => scope.Read("trigger"),
                   scope =>
                   {
                       order.Add(("second sees a", scope.Read("a")));
                       scope.Write("b", 1d);
                   },
                   TriggerMode.Changes);

        graph.Prime();
        graph.Write("trigger", 1d);
        var rounds = graph.Step();

        // neither body saw the other's write, whichever order they ran in
        Assert.Equal([0d, 0d], order.Select(entry => entry.Saw));

        // and the writes did land, in a later round
        Assert.Equal(1d, graph.Read("a"));
        Assert.Equal(1d, graph.Read("b"));
        Assert.True(rounds > 1, $"expected a cascade round, settled in {rounds}");
    }

    // 6 -------------------------------------------------------------------

    [Fact(DisplayName = "a body feeding its own trigger is caught and named")]
    public void ABodyFeedingItsOwnTriggerIsCaughtAndNamed()
    {
        Graph graph = new(cascades: 16);
        graph.Var("temp", 0d);
        graph.When("temp moved",
                   scope => scope.Read("temp"),
                   scope => scope.Write("temp", Add(scope.Read("temp"), 1d)),
                   TriggerMode.Changes);
        graph.Prime();

        graph.Write("temp", 1d);

        var runaway = Assert.Throws<RunawayCascade>(() => graph.Step());
        Assert.Contains("«temp moved»", runaway.Message);
        Assert.Contains("16 rounds", runaway.Message);
    }

    [Fact(DisplayName = "and the fix the message names works")]
    public void AndTheFixTheMessageNamesWorks()
    {
        // The message says to stop the body writing once the condition it acts
        // on is satisfied. A message proposing an edit has to have that edit
        // shown to work, or it is worse than no message at all.
        Graph graph = new(cascades: 16);
        graph.Var("temp", 0d);
        graph.When("temp moved",
                   scope => scope.Read("temp"),
                   scope =>
                   {
                       if ((double)scope.Read("temp") >= 5d) return;
                       scope.Write("temp", Add(scope.Read("temp"), 1d));
                   },
                   TriggerMode.Changes);
        graph.Prime();

        graph.Write("temp", 1d);

        Assert.Equal(5, graph.Step());
        Assert.Equal(5d, graph.Read("temp"));
    }

    // 8 -------------------------------------------------------------------

    [Fact(DisplayName = "one definition covers both branches, and stays findable")]
    public void OneDefinitionCoversBothBranches()
    {
        // What «now let» is usually reaching for, at no cost: dependencies are
        // already dynamic, so only the taken branch is depended on and the
        // definition stays in one searchable place.
        Graph graph = new();
        graph.Var("mode", "light");
        graph.Var("power", 10d);
        graph.Let("damage", scope => (string)scope.Read("mode") is "brutal"
                                   ? Multiply(scope.Read("power"), 5d)
                                   : Add(scope.Read("power"), 1d));

        Assert.Equal(11d, graph.Read("damage"));

        graph.Write("mode", "brutal");
        graph.Step();
        Assert.Equal(50d, graph.Read("damage"));
    }

    // the pieces the scenarios exercise only in passing ---------------------

    [Fact(DisplayName = "a step with no writes still settles and fires")]
    public void AStepWithNoWritesStillSettlesAndFires()
    {
        // A shadow advances at the start of every step with no write behind it,
        // so a «when» reading «old x» can be dirtied by the step itself. Gating
        // the cascade loop on pending writes meant it was dirtied and never
        // looked at.
        Graph graph = new();
        graph.Var("x", 1d);
        graph.Shadow("x");
        graph.Var("log", 0d);
        graph.When("x settled", scope => Equals(scope.Read("x"), scope.Read("old x")),
                   scope => scope.Write("log", Add(scope.Read("log"), 1d)));
        graph.Prime();

        graph.Write("x", 2d);
        graph.Step();                       // old x -> 1, x -> 2: not settled
        Assert.Equal(0d, graph.Read("log"));

        // no writes at all, but «old x» catches up and the condition rises
        graph.Step();

        Assert.Contains("x settled", graph.Fired);
        Assert.Equal(1d, graph.Read("log"));
    }

    [Fact(DisplayName = "a condition that recovers from failure rises")]
    public void AConditionThatRecoversFromFailureRises()
    {
        // A condition is a boolean by construction, so the edge would be false to
        // true — except that a failed condition is neither, and its failure
        // becomes the baseline. Recovering to true is therefore an edge from
        // not-true to true, and it fires. The alternative swallows the first real
        // crossing after any upstream failure, which is the worst possible
        // moment to go quiet.
        Graph graph = new();
        graph.Var("divisor", 0d);
        graph.Var("alarms", 0d);
        graph.Let("ratio", scope => (double)scope.Read("divisor") is 0d
                                  ? new Error("divide by zero")
                                  : 100d / (double)scope.Read("divisor"));
        graph.When("ratio is small",
                   scope => Exceeds(10d, scope.Read("ratio")),
                   scope => scope.Write("alarms", Add(scope.Read("alarms"), 1d)));

        graph.Prime();
        Assert.IsType<Error>(graph.Read("ratio"));

        graph.Write("divisor", 50d);
        graph.Step();

        Assert.Equal(1d, graph.Read("alarms"));
    }

    [Fact(DisplayName = "a failing trigger fires nothing")]
    public void AFailingTriggerFiresNothing()
    {
        Graph graph = new();
        graph.Var("source", 1d);
        graph.Var("ran", 0d);
        graph.When("bad", scope => scope.Read("missing"),   // undeclared: an error
                   scope => scope.Write("ran", 1d),
                   TriggerMode.Changes);
        graph.Prime();

        graph.Write("source", 2d);
        graph.Step();

        Assert.Empty(graph.Fired);
        Assert.Equal(0d, graph.Read("ran"));
    }
}
