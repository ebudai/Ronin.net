// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Runtime;

namespace Unit;

/// <summary>
///     «old (_)» as a constrained pattern, and the shadow cell it allocates.
/// </summary>
[Trait(nameof(Graph), null)]
public class Shadows
{
    private static readonly Func<object, object, object> Add
        = Builtin.Lift((left, right) => (double)left + (double)right);

    [Fact(DisplayName = "old is a pattern rather than an injected name")]
    public void OldIsAPatternRatherThanAnInjectedName()
    {
        SymbolTable symbols = new();
        symbols.WithReactives("smoothed", "reading");

        Assert.Equal(["reading", "smoothed"], symbols.Names.Order());

        // The constrained reference fixes the pattern's extent, so arithmetic
        // cannot be swallowed into the hole: this is (old smoothed) * ..., not
        // old (smoothed * ...).
        Resolver resolver = new(symbols);
        Assert.Equal("((old «smoothed» * 0.9) + («reading» * 0.1))",
                     resolver.Resolve("old smoothed * 0.9 + reading * 0.1").Reading);
    }

    [Fact(DisplayName = "and evaluation allocates the descriptor's runtime shadow")]
    public void AndEvaluationAllocatesTheDescriptorsRuntimeShadow()
    {
        var injected = Injection.Shadow.Of("reading");
        SymbolTable symbols = new();
        symbols.WithReactives("reading");

        Assert.True(new Resolver(symbols).Resolve("old reading").TryTree(out var tree));

        Graph graph = new();
        graph.Var("reading", 10d);
        var before = graph.Declared;

        Assert.Same(Nothing.Instance, new Evaluator(new Scope()).Evaluate(graph, tree, insideLet: false));
        Assert.Equal(before + 1, graph.Declared);
        Assert.Equal(injected, graph.Shadow("reading"));
    }

    [Fact(DisplayName = "a cell reading its own old is not a cycle")]
    public void ACellReadingItsOwnOldIsNotACycle()
    {
        SymbolTable symbols = new();
        symbols.WithReactives("smoothed", "reading");

        Assert.True(new Resolver(symbols).Resolve("old smoothed otherwise 0 + reading").TryTree(out var tree));

        Graph graph = new();
        graph.Var("reading", 10d);
        graph.Let("smoothed", new Evaluator(new Scope()).Body(tree));

        Assert.Equal(10d, graph.Read("smoothed"));
        Assert.Contains("old smoothed", graph.Dependencies("smoothed"));
        Assert.DoesNotContain("smoothed", graph.Dependencies("smoothed"));
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

    [Fact(DisplayName = "old is an ordinary pattern segment now")]
    public void OldIsAnOrdinaryPatternSegmentNow()
    {
        Assert.Empty(Rules.Validate([Declares("smoothed")], [Shape("recall _ old _")]));
    }

    [Fact(DisplayName = "old takes a bare reactive reference")]
    public void OldTakesABareReactiveReference()
    {
        SymbolTable symbols = new();
        symbols.WithReactives("x").WithNames("y").Constants("pi");
        Resolver resolver = new(symbols);

        Assert.Equal("Resolved", resolver.Resolve("old x").Kind.ToString());
        Assert.Equal("Resolved", resolver.Resolve("old (x)").Kind.ToString());
        Assert.Equal("(old «x» + 1)", resolver.Resolve("old x + 1").Reading);

        Assert.Equal("NoParse", resolver.Resolve("old (x + 1)").Kind.ToString());
        Assert.Equal("NoParse", resolver.Resolve("old y").Kind.ToString());
        Assert.Equal("NoParse", resolver.Resolve("old pi").Kind.ToString());

        // An EMPTY hole, which the bracketed form can reach and the bare one
        // cannot: stripping the brackets leaves nothing between them, and a
        // pattern whose argument is no words at all has no reference to be
        // constrained to.
        Assert.Equal("NoParse", resolver.Resolve("old ()").Kind.ToString());

        Assert.Contains("not reactive", symbols.Explain("old y"));
        Assert.Contains("constant", symbols.Explain("old pi"));

        // And SILENT where there is nothing better to say than what the caller
        // already knows. «old x» reads, so there is nothing to explain; «old
        // zzz» fails because «zzz» is not a name, which is the ordinary missing
        // name the caller reports anyway. Explaining either would be inventing a
        // reason for something that has one.
        Assert.Null(symbols.Explain("old x"));
        Assert.Null(symbols.Explain("old zzz"));
    }

    [Fact(DisplayName = "and brackets select it from a comparison")]
    public void AndBracketsSelectItFromAComparison()
    {
        SymbolTable symbols = new();
        symbols.WithNames("old", "valid").WithReactives("is valid");
        Resolver resolver = new(symbols);

        var ambiguous = resolver.Resolve("old is valid");

        Assert.Equal("Ambiguous", ambiguous.Kind.ToString());
        Assert.Equal(["old «is valid»", "(«old» is «valid»)"], ambiguous.Readings);
        Assert.Equal("old ⟨«is valid»⟩", resolver.Resolve("old (is valid)").Reading);
        Assert.Equal("(⟨«old»⟩ is ⟨«valid»⟩)", resolver.Resolve("(old) is (valid)").Reading);
    }

    [Fact(DisplayName = "the old pattern reserves its name prefix")]
    public void TheOldPatternReservesItsNamePrefix()
    {
        var finding = Assert.IsType<NameShadowsPattern>(Assert.Single(
            Compilation.Of(new SourceText("let old smoothed => reactive Number;\n", "Player.ron")).Findings));

        Assert.True(finding.Builtin);
        Assert.Equal("old smoothed", finding.Name);
        Assert.Equal("old (_)", finding.Pattern);
        Assert.Empty(finding.Related);

        // Proper prefix only: the word itself cannot cover a call with an
        // argument, so there is no rival reading to refuse.
        Assert.Empty(Compilation.Of(new SourceText("var old => Number;\n", "Player.ron")).Findings);
    }

    [Fact(DisplayName = "and its exact shape cannot be redeclared")]
    public void AndItsExactShapeCannotBeRedeclared()
    {
        var finding = Assert.IsType<Supplied>(Assert.Single(
            Compilation.Of(new SourceText("function old (value => Number) { return value; }\n",
                                          "Player.ron")).Findings));

        Assert.Equal("old (_)", finding.Pattern);
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
    [InlineData("constant otherwise things = 1;\n"
              + "function send (x => Number) to (y => Number) { return x; }\n"
              + "function send (x => Number) to otherwise (y => Number) { return x; }\n",
                nameof(InfixInPattern))]
    public void APatternWrongInItselfReservesNothingAgainstAnyone(string source, string only)
    {
        // Found by audit. The «sound» filter's own comment states the invariant
        // for every rule — a pattern wrong in itself does not then get to
        // reserve words — and it was applied to the glue scan alone. So a
        // pattern already refused for an operator word went on reserving a
        // prefix, adding a finding whose repair the structural one already
        // states.
        var findings = Compilation.Of(new SourceText(source, "Player.ron")).Findings;

        Assert.Equal(only, Assert.Single(findings).GetType().Name);
    }

}
