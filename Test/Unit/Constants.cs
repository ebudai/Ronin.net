// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Runtime;

namespace Unit;

/// <summary>
///     A constant is not a node.
/// </summary>
[Trait(nameof(Graph), null)]
public class Constants
{
    private static readonly SourceText Nowhere = new(string.Empty);

    private static IReadOnlySet<string> Reads(params string[] names) => new HashSet<string>(names);

    [Fact(DisplayName = "reading a constant creates no edge")]
    public void ReadingAConstantCreatesNoEdge()
    {
        // A constant can never change, so it can never mark anything dirty, and
        // an edge into one is memory held for an impossible event. In a UI or
        // game program those edges would be most of the graph and none of the
        // behaviour.
        Graph graph = new();
        graph.Constant("pi", 3.14d);
        graph.Var("radius", 2d);
        graph.Let("area", scope => (double)scope.Read("pi") * (double)scope.Read("radius"));

        Assert.Equal(6.28d, graph.Read("area"));

        // only the var is depended on
        Assert.Equal(["radius"], graph.Dependencies("area"));
    }

    [Fact(DisplayName = "a constant is not a node at all")]
    public void AConstantIsNotANodeAtAll()
    {
        // Not "a var that refuses writes": there is no node, so no write path
        // exists to refuse, and it can never appear in a dirty set or a ring.
        Graph graph = new();
        graph.Constant("pi", 3.14d);

        Assert.Equal(3.14d, graph.Read("pi"));
        Assert.Throws<KeyNotFoundException>(() => graph.Dependencies("pi"));
    }

    [Fact(DisplayName = "a name is declared once across both stores")]
    public void ANameIsDeclaredOnceAcrossBothStores()
    {
        // A constant is not a node, and only nodes were checked — so a constant
        // declared over a var hid it outright: Read consults constants first, and
        // the node and all its edges were still there, no longer reachable by
        // name. Not a value that lost a race, a graph with a piece of it walled
        // off.
        Graph over = new();
        over.Var("x", 1d);

        Assert.Throws<InitialisationFailure>(() => over.Constant("x", 2d));
        Assert.Equal(1d, over.Read("x"));

        // and the same hole from the other side
        Graph under = new();
        under.Constant("pi", 3.14d);

        Assert.Throws<InitialisationFailure>(() => under.Var("pi", 3d));
        Assert.Throws<InitialisationFailure>(() => under.Constant("pi", 3d));
        Assert.Equal(3.14d, under.Read("pi"));
    }

    [Fact(DisplayName = "a constant whose initialiser failed stops the program")]
    public void AConstantWhoseInitialiserFailedStopsTheProgram()
    {
        // Nothing recomputes a constant, so the error can never clear: it would
        // latch and every reader would inherit it forever. Same argument that
        // decided a shadow seeds with nothing.
        Graph graph = new();

        var failure = Assert.Throws<InitialisationFailure>(
            () => graph.Constant("config", new Error("no such file")));

        Assert.Contains("«config»", failure.Message);
        Assert.Contains("can never clear", failure.Message);
    }

    [Fact(DisplayName = "a constant gets no shadow, and the diagnostic says why")]
    public void AConstantGetsNoShadowAndTheDiagnosticSaysWhy()
    {
        SymbolTable symbols = new();
        symbols.Constants("pi").Declaring("reading");

        Assert.Equal(["old reading", "pi", "reading"], symbols.Names.Order());

        // «old pi» would be a synonym that looks like it means something
        Assert.Equal(
            "no name «old pi» in scope. «pi» is a constant, so it has no previous value — use «pi».",
            symbols.Explain("old pi"));

        // and there is nothing to say about names that are simply absent
        Assert.Null(symbols.Explain("old reading"));
        Assert.Null(symbols.Explain("bogus"));
    }

    [Fact(DisplayName = "constants and vars are ordered in one graph")]
    public void ConstantsAndVarsAreOrderedInOneGraph()
    {
        // Ordering constants among themselves never places «initial health»,
        // because «health» is a var and so is not in that graph at all. One graph
        // makes the snapshot well-defined instead of order-dependent, which is
        // the static-initialisation-order trap.
        Dictionary<string, IReadOnlySet<string>> initialisers = new()
        {
            ["detail level"] = Reads(),
            ["pi"] = Reads(),
            ["circle segments"] = Reads("detail level", "pi"),
            ["difficulty"] = Reads(),
            ["max health"] = Reads("difficulty"),
            ["health"] = Reads("max health"),
            ["initial health"] = Reads("health"),
        };

        Assert.True(Initialisation.TryOrder(initialisers, out var order));

        foreach (var (name, reads) in initialisers)
        {
            foreach (var read in reads)
            {
                Assert.True(order.ToList().IndexOf(read) < order.ToList().IndexOf(name),
                            $"«{read}» must be evaluated before «{name}»");
            }
        }

        // the snapshot lands after the var it captures, which is the whole point
        Assert.Equal("initial health", order[^1]);
    }

    [Fact(DisplayName = "a cycle across the mixed set has no order")]
    public void ACycleAcrossTheMixedSetHasNoOrder()
    {
        // four hops, three declaration kinds, one detector pointed at a
        // different node set
        Dictionary<string, IReadOnlySet<string>> initialisers = new()
        {
            ["difficulty"] = Reads("max health"),
            ["initial health"] = Reads("difficulty"),
            ["health"] = Reads("initial health"),
            ["max health"] = Reads("health"),
        };

        Assert.False(Initialisation.TryOrder(initialisers, out var order));
        Assert.Empty(order);

        var complaint = Assert.Single(Initialisation.Diagnose(
            initialisers.ToDictionary(entry => new Declared(entry.Key, Nowhere.Span(0, 0)), entry => entry.Value)));

        Assert.Equal(FindingKind.InitialisationRing, complaint.Kind);
        Assert.Equal("difficulty» → «initial health» → «health» → «max health» → «difficulty",
                     Assert.IsType<InitialisationRing>(complaint).Ring);

        // four hops, three declaration kinds — every one of them named
        Assert.Equal(3, complaint.Related.Count);
    }

    [Fact(DisplayName = "reading outside the set places nothing")]
    public void ReadingOutsideTheSetPlacesNothing()
    {
        // a literal, a pattern call or a name from an enclosing scope is already
        // there and needs no placing
        Dictionary<string, IReadOnlySet<string>> initialisers = new()
        {
            ["greeting"] = Reads("elsewhere"),
        };

        Assert.True(Initialisation.TryOrder(initialisers, out var order));
        Assert.Equal(["greeting"], order);
    }
}
