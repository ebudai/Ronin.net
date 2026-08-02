using Ronin.Runtime;

namespace Unit;

public sealed class AuditProbe
{
    [Fact]
    public void AFailedBodyDoesNotApplyItsStop()
    {
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
    }

    [Fact]
    public void AnUnrelatedInheritedRunMustNotExemptNewWork()
    {
        Graph graph = new(cascades: 2);
        graph.Var("old head", false);
        graph.Var("never", false);
        graph.Var("new head", false);

        graph.Chain("old chain",
                    (scope => scope.Read("old head"), _ => { }),
                    (scope => scope.Read("never"), _ => { }));

        graph.Chain("new chain",
                    (scope => scope.Read("new head"), _ => { }),
                    (_ => true, _ => { }));

        graph.Prime();

        graph.Write("old head", true);
        graph.Step();
        graph.Write("old head", false);
        graph.Step();

        Assert.Equal(1d, graph.Read(Graph.Waiting("old chain", 1)));

        graph.Write("new head", true);

        Assert.Throws<RunawayCascade>(() => graph.Step());
    }

    [Fact]
    public void ControlWithoutTheUnrelatedRunReachesTheLimit()
    {
        Graph graph = new(cascades: 2);
        graph.Var("new head", false);
        graph.Chain("new chain",
                    (scope => scope.Read("new head"), _ => { }),
                    (_ => true, _ => { }));
        graph.Prime();
        graph.Write("new head", true);

        Assert.Throws<RunawayCascade>(() => graph.Step());
    }
}
