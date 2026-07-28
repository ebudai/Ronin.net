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

    [Fact(DisplayName = "anything that is not true can rise to true")]
    public void AnythingThatIsNotTrueCanRiseToTrue()
    {
        // «becomes true» is an edge on a condition, so the edge is from not-true
        // to true rather than from false to true. A value that is not a boolean
        // has not become true and cannot masquerade as though it had — whether a
        // non-boolean condition should be a louder error is open.
        Graph graph = new();
        graph.Var("step", 0d);
        graph.Var("fired", 0d);
        graph.When("odd shape",
                   scope => (double)scope.Read("step") is 0d ? "not a condition" : (object)true,
                   scope => scope.Write("fired", Add(scope.Read("fired"), 1d)));
        graph.Prime();

        graph.Write("step", 1d);
        graph.Step();

        Assert.Equal(1d, graph.Read("fired"));
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
