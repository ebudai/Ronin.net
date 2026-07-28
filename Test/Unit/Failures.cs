// Copyright © 2026 Eric Budai

using Ronin.Runtime;

namespace Unit;

/// <summary>
///     The two kinds of failure, from <c>docs/handoff/error_model.py</c>.
/// </summary>
///
/// <remarks>
///     Lift and adoption do different jobs and neither covers the other. Lift
///     keeps an error inert inside a body so the arithmetic never raises;
///     adoption guarantees the node inherits it whatever the body does with it.
/// </remarks>
[Trait(nameof(Graph), null)]
public class Failures
{
    private static readonly Func<object, object, object> Add
        = Builtin.Lift((left, right) => (double)left + (double)right);

    private static Graph Failing()
    {
        Graph graph = new();
        graph.Var("divisor", 0d);
        graph.Let("ratio", scope => (double)scope.Read("divisor") is 0d
                                  ? new Error("divide by zero")
                                  : 100d / (double)scope.Read("divisor"));
        return graph;
    }

    [Fact(DisplayName = "a body cannot discard an error it read")]
    public void ABodyCannotDiscardAnErrorItRead()
    {
        // lift alone is not enough: no operator is involved here, so nothing
        // would ever have seen the error
        var graph = Failing();
        graph.Let("sloppy", scope =>
        {
            scope.Read("ratio");
            return 42d;
        });

        Assert.IsType<Error>(graph.Read("sloppy"));

        // and it clears when the source is fixed, like any other error
        graph.Write("divisor", 4d);
        graph.Step();
        Assert.Equal(42d, graph.Read("sloppy"));
    }

    [Fact(DisplayName = "adoption is not enough either: lift keeps errors inert")]
    public void AdoptionIsNotEnoughEither()
    {
        // raw arithmetic on an error raises, and the graph can only report that
        // as a fault — a program error given the wrong diagnosis. Lift is what
        // stops the body exploding in the first place.
        var graph = Failing();
        graph.Let("raw", scope => (double)scope.Read("ratio") + 1d);

        Assert.IsType<Fault>(graph.Read("raw"));

        graph.Let("lifted", scope => Add(scope.Read("ratio"), 1d));
        Assert.IsType<Error>(graph.Read("lifted"));
        Assert.IsNotType<Fault>(graph.Read("lifted"));
    }

    [Fact(DisplayName = "an interpreter defect is a fault, not a program error")]
    public void AnInterpreterDefectIsAFaultNotAProgramError()
    {
        // the session survives one bad node, but a null reference in the
        // evaluator must never surface as a user-facing spreadsheet error
        Graph graph = new();
        graph.Let("buggy", _ => throw new InvalidOperationException("interpreter bug"));

        var fault = Assert.IsType<Fault>(graph.Read("buggy"));
        Assert.Contains("InvalidOperationException", fault.Message);
        Assert.StartsWith("fault(", fault.ToString());
    }

    [Fact(DisplayName = "otherwise catches an error and never a fault")]
    public void OtherwiseCatchesAnErrorAndNeverAFault()
    {
        // a fallback for a program error is a fallback; a fallback for an
        // interpreter bug is a hidden crash
        Assert.Equal(0d, Builtin.Otherwise(new Error("divide by zero"), 0d));
        Assert.Equal(0d, Builtin.Otherwise(Nothing.Instance, 0d));

        var fault = new Fault("NullReferenceException: x");
        Assert.Same(fault, Builtin.Otherwise(fault, 0d));
    }

    [Fact(DisplayName = "a fault propagates to whoever read it")]
    public void AFaultPropagatesToWhoeverReadIt()
    {
        // ignoring a fault must not launder it into a value, for the same reason
        // ignoring an error must not
        Graph graph = new();
        graph.Let("buggy", _ => throw new InvalidOperationException("interpreter bug"));
        graph.Let("ignores", scope =>
        {
            scope.Read("buggy");
            return 42d;
        });

        Assert.IsType<Fault>(graph.Read("ignores"));
    }

    [Fact(DisplayName = "a plain read defeats otherwise, and handling is why")]
    public void APlainReadDefeatsOtherwiseAndHandlingIsWhy()
    {
        // The hazard, not just the fix: read plainly and adoption overrides the
        // fallback with the very error otherwise just handled. Anyone
        // "simplifying" the call site would silently break the one thing that
        // catches, so the failure is pinned here beside the remedy.
        Graph graph = new();
        graph.Var("parsed", Nothing.Instance);
        graph.Let("naive", scope => Builtin.Otherwise(scope.Read("parsed"), 0d));
        graph.Let("correct", scope => Builtin.Otherwise(scope.Handling(() => scope.Read("parsed")), 0d));

        graph.Write("parsed", new Error("bad input"));
        graph.Step();

        Assert.IsType<Error>(graph.Read("naive"));
        Assert.Equal(0d, graph.Read("correct"));
    }

    [Fact(DisplayName = "a fault is not overwritten by adoption")]
    public void AFaultIsNotOverwrittenByAdoption()
    {
        // reading an error and then failing for an unrelated reason is still a
        // fault: the defect is the more urgent report
        var graph = Failing();
        graph.Let("both", scope =>
        {
            scope.Read("ratio");
            throw new InvalidOperationException("interpreter bug");
        });

        Assert.IsType<Fault>(graph.Read("both"));
    }
}
