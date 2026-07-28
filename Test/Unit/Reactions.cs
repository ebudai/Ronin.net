// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Runtime;

namespace Unit;

/// <summary>
///     The 26 scenarios from <c>docs/handoff/reactive_scenarios.py</c>, which pin
///     every interpreter decision. Ported in the order the decisions depend on
///     each other: the var/let split and purity first, since everything rests on
///     them, then dynamic edges and glitch freedom, then calls, then cycles,
///     errors and batching.
/// </summary>
[Trait(nameof(Graph), null)]
public class Reactions
{
    private static readonly Func<object, object, object> Add
        = Builtin.Lift((left, right) => (double)left + (double)right);

    private static readonly Func<object, object, object> Multiply
        = Builtin.Lift((left, right) => (double)left * (double)right);

    // 1 -------------------------------------------------------------------

    [Fact(DisplayName = "a var is a source and a let is derived")]
    public void AVarIsASourceAndALetIsDerived()
    {
        Graph graph = new();
        graph.Var("base price", 100d);
        graph.Var("tax rate", 0.2d);
        graph.Let("total", scope => Add(scope.Read("base price"),
                                        Multiply(scope.Read("base price"), scope.Read("tax rate"))));

        Assert.Equal(120d, graph.Read("total"));

        graph.Write("base price", 200d);
        Assert.Equal(120d, graph.Read("total"));   // invisible before the step

        graph.Step();
        Assert.Equal(240d, graph.Read("total"));
    }

    // 2 -------------------------------------------------------------------

    [Fact(DisplayName = "nothing recomputes when nothing it reads changed")]
    public void NothingRecomputesWhenNothingItReadsChanged()
    {
        Graph graph = new();
        graph.Var("tax rate", 0.2d);
        graph.Let("total", scope => Multiply(scope.Read("tax rate"), 100d));

        graph.Read("total");
        graph.Forget();

        graph.Read("total");
        Assert.Empty(graph.Trace);

        graph.Write("tax rate", 0.2d);   // the same value
        graph.Step();
        Assert.Empty(graph.Trace);
    }

    // 9 -------------------------------------------------------------------

    [Fact(DisplayName = "purity is enforced, not assumed")]
    public void PurityIsEnforcedNotAssumed()
    {
        Graph graph = new();
        graph.Var("counter", 0d);
        graph.Let("bad", scope =>
        {
            scope.Write("counter", 1d);   // a let may not assign a var
            return 1d;
        });

        var error = Assert.IsType<Error>(graph.Read("bad"));
        Assert.Contains("may not assign", error.Message);
    }

    [Fact(DisplayName = "only a let's own body may set it")]
    public void OnlyALetsOwnBodyMaySetIt()
    {
        Graph graph = new();
        graph.Let("derived", _ => 1d);

        var violation = Assert.Throws<PurityViolation>(() => graph.Write("derived", 2d));
        Assert.Contains("only its body may set it", violation.Message);
    }

    // 3 -------------------------------------------------------------------

    [Fact(DisplayName = "dependencies are recorded, never read off the tree")]
    public void DependenciesAreRecordedNeverReadOffTheTree()
    {
        Graph graph = new();
        graph.Var("use metric", true);
        graph.Var("metres", 100d);
        graph.Var("feet", 328d);
        graph.Let("distance", scope => (bool)scope.Read("use metric") ? scope.Read("metres") : scope.Read("feet"));

        graph.Read("distance");
        Assert.Equal(["metres", "use metric"], graph["distance"].Dependencies.Order());

        graph.Write("use metric", false);
        graph.Step();
        graph.Read("distance");
        Assert.Equal(["feet", "use metric"], graph["distance"].Dependencies.Order());

        // the branch it no longer looks at must not wake it
        graph.Forget();
        graph.Write("metres", 999d);
        graph.Step();
        Assert.Empty(graph.Trace);
    }

    // 4 -------------------------------------------------------------------

    [Fact(DisplayName = "a diamond evaluates its shared parent once")]
    public void ADiamondEvaluatesItsSharedParentOnce()
    {
        Graph graph = new();
        graph.Var("x", 1d);
        graph.Let("a", scope => Add(scope.Read("x"), 1d));
        graph.Let("b", scope => Multiply(scope.Read("a"), 2d));
        graph.Let("c", scope => Add(scope.Read("a"), 10d));
        graph.Let("d", scope => Add(scope.Read("b"), scope.Read("c")));

        Assert.Equal((2d * 2d) + (2d + 10d), graph.Read("d"));

        graph.Write("x", 5d);
        graph.Step();
        graph.Forget();

        Assert.Equal((6d * 2d) + (6d + 10d), graph.Read("d"));
        Assert.Single(graph.Trace, name => name is "a");
    }

    // 10 ------------------------------------------------------------------

    private static Scope Declared()
    {
        Scope scope = new();

        scope.Declare(new Declaration(
            Pattern.Parse("compute total for _"),
            [["order"]],
            (_, bound) => Multiply(bound["order"], 2d)));

        scope.Declare(new Declaration(
            Pattern.Parse("draw _ at _"),
            [["shape"], ["x", "y"]],           // the second block binds two
            (_, bound) => $"{bound["shape"]}@{bound["x"]},{bound["y"]}"));

        scope.Declare(new Declaration(
            Pattern.Parse("save _"),
            [["data"]],
            (_, bound) => $"wrote {bound["data"]}",
            pure: false));

        return scope;
    }

    [Fact(DisplayName = "a call binds its blocks")]
    public void ACallBindsItsBlocks()
    {
        var scope = Declared();
        Graph graph = new();

        Assert.Equal(42d, scope.Invoke(graph, Pattern.Parse("compute total for _"), [21d], insideLet: false));

        Assert.Equal("circle@3,4",
                     scope.Invoke(graph, Pattern.Parse("draw _ at _"),
                                  ["circle", (object[])[3d, 4d]], insideLet: false));
    }

    [Fact(DisplayName = "a call rejects what it cannot bind or run")]
    public void ACallRejectsWhatItCannotBindOrRun()
    {
        var scope = Declared();
        Graph graph = new();

        // a block of two given a group of one, and given no group at all — the
        // brackets may be dropped for a single parameter, never for a pair
        var short_ = Assert.IsType<Error>(scope.Invoke(graph, Pattern.Parse("draw _ at _"),
                                                       ["circle", (object[])[3d]], insideLet: false));
        Assert.Contains("was given 1", short_.Message);

        var unbracketed = Assert.IsType<Error>(scope.Invoke(graph, Pattern.Parse("draw _ at _"),
                                                            ["circle", 3d], insideLet: false));
        Assert.Contains("a single argument", unbracketed.Message);

        // effects are what the purity rule excludes from a let
        Assert.IsType<Error>(scope.Invoke(graph, Pattern.Parse("save _"), ["x"], insideLet: true));
        Assert.Equal("wrote x", scope.Invoke(graph, Pattern.Parse("save _"), ["x"], insideLet: false));

        // bodies never run on error inputs
        Assert.IsType<Error>(scope.Invoke(graph, Pattern.Parse("compute total for _"),
                                          [new Error("upstream")], insideLet: false));
    }

    [Fact(DisplayName = "a call needs exactly one declaration")]
    public void ACallNeedsExactlyOneDeclaration()
    {
        Scope scope = new();
        Graph graph = new();

        var undeclared = Assert.IsType<Error>(
            scope.Invoke(graph, Pattern.Parse("no such _"), [1d], insideLet: false));
        Assert.Contains("no declaration", undeclared.Message);

        // overloads share a shape and are separated by type; one that survives
        // the type filter twice is a tie, and a tie is an error
        scope.Declare(new Declaration(Pattern.Parse("twice _"), [["a"]], (_, _) => 1d));
        scope.Declare(new Declaration(Pattern.Parse("twice _"), [["a"]], (_, _) => 2d));

        var ambiguous = Assert.IsType<Error>(
            scope.Invoke(graph, Pattern.Parse("twice _"), [1d], insideLet: false));
        Assert.Contains("ambiguous", ambiguous.Message);
    }

    [Fact(DisplayName = "a declaration needs one block per hole")]
    public void ADeclarationNeedsOneBlockPerHole()
    {
        Assert.Throws<ArgumentNullException>(() => new Declaration(null, [], (_, _) => 1d));
        Assert.Throws<ArgumentNullException>(() => new Declaration(Pattern.Parse("a _"), null, (_, _) => 1d));
        Assert.Throws<ArgumentNullException>(() => new Declaration(Pattern.Parse("a _"), [["x"]], null));

        var mismatch = Assert.Throws<ArgumentException>(
            () => new Declaration(Pattern.Parse("draw _ at _"), [["shape"]], (_, _) => 1d));
        Assert.Contains("2 hole(s) and 1 block(s)", mismatch.Message);
    }

    [Fact(DisplayName = "a scope rejects nonsense")]
    public void AScopeRejectsNonsense()
    {
        Scope scope = new();
        Graph graph = new();

        Assert.Throws<ArgumentNullException>(() => scope.Declare(null));
        Assert.Throws<ArgumentNullException>(() => scope.Invoke(graph, null, [], insideLet: false));
        Assert.Throws<ArgumentNullException>(() => scope.Invoke(graph, Pattern.Parse("a _"), null, insideLet: false));
    }

    // 5 -------------------------------------------------------------------

    [Fact(DisplayName = "a cycle is an error, detected by re-entry")]
    public void ACycleIsAnErrorDetectedByReEntry()
    {
        Graph graph = new();
        graph.Let("p", scope => Add(scope.Read("q"), 1d));
        graph.Let("q", scope => Add(scope.Read("p"), 1d));

        var error = Assert.IsType<Error>(graph.Read("p"));
        Assert.Contains("cycle through", error.Message);
    }

    // 6 -------------------------------------------------------------------

    [Fact(DisplayName = "errors propagate as values and clear on their own")]
    public void ErrorsPropagateAsValuesAndClearOnTheirOwn()
    {
        Graph graph = new();
        graph.Var("divisor", 0d);
        graph.Let("ratio", scope => (double)scope.Read("divisor") is 0d
                                  ? new Error("divide by zero")
                                  : 100d / (double)scope.Read("divisor"));
        graph.Let("report", scope => Add(scope.Read("ratio"), 1d));

        Assert.IsType<Error>(graph.Read("report"));

        graph.Write("divisor", 4d);
        graph.Step();
        Assert.Equal(26d, graph.Read("report"));
    }

    // 7 -------------------------------------------------------------------

    [Fact(DisplayName = "otherwise is the only thing that catches")]
    public void OtherwiseIsTheOnlyThingThatCatches()
    {
        Graph graph = new();
        graph.Var("parsed", Nothing.Instance);
        graph.Let("count", scope => Builtin.Otherwise(scope.Read("parsed"), 0d));

        Assert.Equal(0d, graph.Read("count"));

        graph.Write("parsed", new Error("bad input"));
        graph.Step();
        Assert.Equal(0d, graph.Read("count"));

        graph.Write("parsed", 7d);
        graph.Step();
        Assert.Equal(7d, graph.Read("count"));
    }

    // 8 -------------------------------------------------------------------

    [Fact(DisplayName = "writes batch into one consistent view")]
    public void WritesBatchIntoOneConsistentView()
    {
        Graph graph = new();
        graph.Var("width", 2d);
        graph.Var("height", 3d);

        List<(object Width, object Height)> seen = [];
        graph.Let("area", scope =>
        {
            var width = scope.Read("width");
            var height = scope.Read("height");
            seen.Add((width, height));
            return Multiply(width, height);
        });

        graph.Read("area");
        seen.Clear();

        graph.Write("width", 10d);
        graph.Write("height", 20d);
        graph.Step();

        Assert.Equal(200d, graph.Read("area"));
        Assert.Equal([(10d, 20d)], seen);   // never observed half updated
    }

    // the pieces the scenarios exercise only in passing ---------------------

    [Fact(DisplayName = "a value describes itself")]
    public void AValueDescribesItself()
    {
        Assert.Equal("error(bad input)", new Error("bad input").ToString());
        Assert.Equal("nothing", Nothing.Instance.ToString());
    }

    [Fact(DisplayName = "a lifted operation runs only on good values")]
    public void ALiftedOperationRunsOnlyOnGoodValues()
    {
        var error = new Error("upstream");

        Assert.Equal(3d, Add(1d, 2d));
        Assert.Same(error, Add(error, 2d));
        Assert.Same(error, Add(1d, error));
    }
}
