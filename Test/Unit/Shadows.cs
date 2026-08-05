// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Runtime;

namespace Unit;

/// <summary>
///     «old x» as an injected name, and the shadow cell it allocates.
/// </summary>
[Trait(nameof(Graph), null)]
public class Shadows
{
    private static readonly Func<object, object, object> Add
        = Builtin.Lift((left, right) => (double)left + (double)right);

    [Fact(DisplayName = "declaring a cell injects its shadow")]
    public void DeclaringACellInjectsItsShadow()
    {
        SymbolTable symbols = new();
        symbols.Declaring("smoothed", "reading");

        Assert.Equal(["old reading", "old smoothed", "reading", "smoothed"], symbols.Names.Order());

        // and the injected name is an ordinary name, so it is an operand at every
        // binding level with nothing added to the resolver
        Resolver resolver = new(symbols);
        Assert.Equal("((«old smoothed» * 0.9) + («reading» * 0.1))",
                     resolver.Resolve("old smoothed * 0.9 + reading * 0.1").Reading);
    }

    [Fact(DisplayName = "and the name it injects is the descriptor's, in both halves")]
    public void AndTheNameItInjectsIsTheDescriptorsInBothHalves()
    {
        // Found by audit, and the point of the finding is that the test above
        // cannot see it: it spells «old reading» by hand, so the resolver and the
        // runtime could each hold their own copy of the word and it would still
        // pass. Changing the descriptor would then move the diagnostics, the
        // protection rule and the generated registry and leave these two behind.
        //
        // So this asserts the joins rather than the spelling: whatever the
        // descriptor says, that is what goes in scope and that is what gets
        // allocated.
        var injected = Injection.Shadow.Of("reading");

        Assert.Contains(injected, new SymbolTable().Declaring("reading").Names);

        Graph graph = new();
        graph.Var("reading", 10d);

        Assert.Equal(injected, graph.Shadow("reading"));
    }

    [Fact(DisplayName = "a cell reading its own old is not a cycle")]
    public void ACellReadingItsOwnOldIsNotACycle()
    {
        // «old x» IS a different cell, so the edge lands on the shadow. Not a
        // self-cycle by construction rather than by exemption.
        Graph graph = new();
        graph.Var("reading", 10d);
        graph.Let("smoothed", scope => Add(Builtin.Otherwise(scope.Read("old smoothed"), 0d),
                                           scope.Read("reading")));
        graph.Shadow("smoothed");

        Assert.Equal(10d, graph.Read("smoothed"));
        Assert.Contains("old smoothed", graph.Dependencies("smoothed"));
    }

    [Fact(DisplayName = "a shadow holds the previous step's value all step long")]
    public void AShadowHoldsThePreviousStepsValueAllStepLong()
    {
        Graph graph = new();
        graph.Var("x", 1d);
        graph.Shadow("x");
        graph.Let("moved by", scope => Add(scope.Read("x"),
                                           Builtin.Otherwise(scope.Read("old x"), 0d)));

        // seeded with nothing, never with an error: an error seed latches
        Assert.Same(Nothing.Instance, graph.Read("old x"));
        Assert.Equal(1d, graph.Read("moved by"));

        graph.Write("x", 5d);
        graph.Step();

        Assert.Equal(1d, graph.Read("old x"));
        Assert.Equal(6d, graph.Read("moved by"));

        graph.Write("x", 20d);
        graph.Step();

        Assert.Equal(5d, graph.Read("old x"));
    }

    [Fact(DisplayName = "a shadow is not writable")]
    public void AShadowIsNotWritable()
    {
        Graph graph = new();
        graph.Var("x", 1d);
        graph.Shadow("x");

        var violation = Assert.Throws<PurityViolation>(() => graph.Write("old x", 2d));
        Assert.Contains("previous value of «x»", violation.Message);
    }

    [Fact(DisplayName = "a shadow is allocated once")]
    public void AShadowIsAllocatedOnce()
    {
        Graph graph = new();
        graph.Var("x", 1d);

        var before = graph.Declared;

        graph.Shadow("x");

        var once = graph.Declared;

        graph.Shadow("x");

        // The NODE count, not the returned handle. This asked «Assert.Same» on
        // what «Shadow» handed back, which meant the second call had to return
        // the live node for the test to say anything — the claim is that one
        // node exists however many times it is asked for, and that is what the
        // count says without a caller holding graph state.
        Assert.Equal(before + 1, once);
        Assert.Equal(once, graph.Declared);
    }

    [Fact(DisplayName = "a let reading its own old advances only when observed")]
    public void ALetReadingItsOwnOldAdvancesOnlyWhenObserved()
    {
        // Correct for a smoothing filter and wrong for a clock, which is worth
        // saying in the guide before someone files it as a bug: evaluation is
        // demand driven and the shadow copies the cached value, which only moves
        // when the body ran. A real clock is a var the frame loop writes.
        Graph graph = new();
        graph.Var("frame", 0d);
        graph.Let("tick", scope => Add(Builtin.Otherwise(scope.Read("old tick"), 0d), 1d));
        graph.Shadow("tick");

        graph.Write("frame", 1d);
        graph.Step();
        graph.Write("frame", 2d);
        graph.Step();

        // two steps passed and nobody looked, so it stood still
        Assert.Equal(1d, graph.Read("tick"));
    }


    /// <summary>
    ///     Spans for a rule test, which reads symbols and never their positions.
    ///     Rendering from real findings is what the golden file covers.
    /// </summary>
    private static readonly SourceText Nowhere = new(string.Empty);

    private static Declared Declares(string name, string injectedBy = null)
        => new(name, Nowhere.Span(0, 0), injectedBy);

    private static Ronin.Compiler.Shape Shape(string pattern) => new(Pattern.Parse(pattern), Nowhere.Span(0, 0));

    [Fact(DisplayName = "old is reserved against pattern segments")]
    public void OldIsReservedAgainstPatternSegments()
    {
        // One hostile pattern would put «old» in the glue set, and R5 would then
        // reject every injected name in scope.
        var complaint = Assert.Single(Rules.Validate([Declares("smoothed")], [Shape("recall _ old _")]),
                                      finding => finding.Kind is FindingKind.ReservedSegment);

        var reserved = Assert.IsType<ReservedSegment>(complaint);

        Assert.Equal("old", reserved.Word);
        Assert.Equal("recall (_) old (_)", reserved.Pattern);
    }

    [Fact(DisplayName = "a collision with an injected name is a declaration error")]
    public void ACollisionWithAnInjectedNameIsADeclarationError()
    {
        SymbolTable symbols = new();
        symbols.Declaring("smoothed");

        // the injector is named, because that is the half the programmer forgot
        var collision = Assert.Throws<ArgumentException>(() => symbols.WithNames("old smoothed").Declaring("smoothed"));
        Assert.Contains("declaring «smoothed» injects it", collision.Message);
    }

    [Fact(DisplayName = "there is no old old x")]
    public void ThereIsNoOldOldX()
    {
        SymbolTable symbols = new();

        var refused = Assert.Throws<ArgumentException>(() => symbols.Declaring("old smoothed"));
        Assert.Contains("no «old old x»", refused.Message);
    }

    [Fact(DisplayName = "a changes trigger fires exactly when old disagrees")]
    public void AChangesTriggerFiresExactlyWhenOldDisagrees()
    {
        // «when y changes» and «y is not old y» are the same question asked twice.
        // They are two mechanisms today — a per-when previous field, and a shadow
        // cell — and if they ever disagree about where a step boundary is, the
        // symptoms are miserable. This is the guard on that.
        Graph graph = new();
        graph.Var("y", 1d);
        graph.Shadow("y");
        graph.Var("log", 0d);
        graph.When("y moved", scope => scope.Read("y"),
                   scope => scope.Write("log", Add(scope.Read("log"), 1d)),
                   TriggerMode.Changes);
        graph.Let("y differs", scope => Equals(scope.Read("y"), scope.Read("old y")) is false);
        graph.Prime();

        foreach (var value in new[] { 2d, 2d, 3d, 3d, 4d })
        {
            graph.Write("y", value);
            graph.Step();

            Assert.Equal(graph.Read("y differs"), graph.Fired.Contains("y moved"));
        }

        Assert.Equal(3d, graph.Read("log"));
    }

    [Theory(DisplayName = "a pattern wrong in itself reserves nothing against anyone")]
    [InlineData("var otherwise things => Number;\n"
              + "function send (x => Number) to (y => Number) { return x; }\n"
              + "function send (x => Number) to otherwise (y => Number) { return x; }\n",
                nameof(InfixInPattern))]
    [InlineData("var compute old things => Number;\n"
              + "function compute old (x => Number) { return x; }\n",
                nameof(ReservedSegment))]
    public void APatternWrongInItselfReservesNothingAgainstAnyone(string source, string only)
    {
        // Found by audit. The «sound» filter's own comment states the invariant
        // for every rule — a pattern wrong in itself does not then get to
        // reserve words — and it was applied to the glue scan alone. So a
        // pattern already refused for «old» went on reserving a prefix through
        // R6b, adding a finding whose repair the structural one already states.
        // The R7 half of this went with R7.
        var findings = Compilation.Of(new SourceText(source, "Player.ron")).Findings;

        Assert.Equal(only, Assert.Single(findings).GetType().Name);
    }

}
