// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Runtime;

namespace Unit;

/// <summary>
///     «otherwise» as an infix form: when the left side produced nothing, use
///     this instead.
/// </summary>
///
/// <remarks>
///     <para>
///     A word and not a symbol, and an operator and not a pattern. A pattern
///     spelled «(_) otherwise (_)» has a leading hole, so its anchor run is
///     empty, and R6 refuses an empty run against every anchored pattern there
///     is — which is every real scope. So the language bans word infix as a
///     pattern, and an infix form lives where «+» does.
///     </para>
///     <para>
///     The runtime has had <see cref="Builtin.Otherwise"/> since the error model
///     was built, and the division-by-zero message already tells an author to
///     reach for it. Until now there was no way to write it.
///     </para>
/// </remarks>
[Trait(nameof(Resolver), null)]
public class Fallbacks
{
    private static SymbolTable Names(params string[] names) => new SymbolTable().WithNames(names) as SymbolTable;

    private static Resolution Of(SymbolTable symbols, string source) => new Resolver(symbols).Resolve(source);

    [Theory(DisplayName = "«otherwise» is an operator, and the loosest one")]
    [InlineData("a otherwise b", "(«a» otherwise «b»)")]
    [InlineData("a + b otherwise c", "((«a» + «b») otherwise «c»)")]
    [InlineData("a otherwise b + c", "(«a» otherwise («b» + «c»))")]
    [InlineData("a otherwise b otherwise c", "((«a» otherwise «b») otherwise «c»)")]
    public void OtherwiseIsAnOperatorAndTheLoosestOne(string source, string reading)
    {
        // What it guards is the expression beside it and not the nearest term,
        // which is the only reading that makes it worth writing: «total / count
        // otherwise 0» is a guard on the division, and a fallback that bound
        // tighter than «/» would be a guard on «count».
        Assert.Equal(reading, Of(Names("a", "b", "c"), source).Reading);
    }

    [Theory(DisplayName = "and it needs something on both sides")]
    [InlineData("otherwise a")]
    [InlineData("a otherwise")]
    public void AndItNeedsSomethingOnBothSides(string source)
        => Assert.Equal(ResolutionKind.NoParse, Of(Names("a", "b"), source).Kind);

    [Fact(DisplayName = "a good value is its own answer, and the fallback is never read")]
    public void AGoodValueIsItsOwnAnswerAndTheFallbackIsNeverRead()
    {
        // The half that is not an optimisation. A body's dependencies are
        // collected BY evaluating it, so an operand that is evaluated is a cell
        // that is read and an edge that exists — an eager «otherwise» makes the
        // fallback an input of the very cell it is guarding, and writing to a
        // fallback nobody wanted recomputes it.
        var (graph, recomputed) = Guarded("reading otherwise standby");

        Assert.Equal(20d, graph.Read("guarded"));

        var before = recomputed();

        graph.Write("standby", 99d);
        graph.Step();

        // READ before counting. A cell recomputes when it is pulled and not
        // when it is dirtied, so counting straight after the step counts
        // nothing and passes however the fallback is evaluated.
        Assert.Equal(20d, graph.Read("guarded"));
        Assert.Equal(before, recomputed());
    }

    [Fact(DisplayName = "and a failing one reaches for it, which is when it becomes an input")]
    public void AndAFailingOneReachesForItWhichIsWhenItBecomesAnInput()
    {
        var (graph, recomputed) = Guarded("reading / missing otherwise standby");

        Assert.Equal(5d, graph.Read("guarded"));

        var before = recomputed();

        graph.Write("standby", 99d);
        graph.Step();

        Assert.Equal(99d, graph.Read("guarded"));
        Assert.Equal(before + 1, recomputed());
    }

    [Fact(DisplayName = "a declared name takes the words back, silently")]
    public void ADeclaredNameTakesTheWordsBackSilently()
    {
        // Pinned because it is a silent reading and not a tie. Minimum lookup
        // scores one name below an operation over two, so declaring «x otherwise
        // y» does not make the statement ambiguous — it makes it mean something
        // else, and every statement already written that way changes with it.
        //
        // That is R5's hazard exactly, and R5 has no jurisdiction: it governs
        // pattern GLUE, and this is neither glue nor a pattern. Whether
        // «otherwise» should join the protected words is a question for the
        // designer — it costs an ordinary English word in every program, which
        // is the bill R5 is willing to pay for glue and has never been asked to
        // pay for an operator.
        Assert.Equal("(«x» otherwise «y»)", Of(Names("x", "y"), "x otherwise y").Reading);

        var shadowed = Of(Names("x", "y", "x otherwise y"), "x otherwise y");

        Assert.Equal(ResolutionKind.Resolved, shadowed.Kind);
        Assert.Equal("«x otherwise y»", shadowed.Reading);
    }

    [Fact(DisplayName = "an error read from another cell is caught, which is the ordinary case")]
    public void AnErrorReadFromAnotherCellIsCaughtWhichIsTheOrdinaryCase()
    {
        // Found by audit. The graph remembers the first error a body READS and
        // applies it to whatever the body returns, so «otherwise» chose the
        // fallback and had the choice overwritten by the very error it was asked
        // to replace. Graph.Handling is the boundary for exactly this and its
        // own summary says so by name; the evaluator never called it.
        //
        // The maintained test could not see it: it divides by zero, which MAKES
        // an error inside the expression rather than reading one, so adoption
        // never armed. A test one join short of the thing it names.
        var (graph, _) = Guarded("failing otherwise standby");

        Assert.Equal(5d, graph.Read("guarded"));
    }

    [Fact(DisplayName = "and one that a derived cell computed, when that cell is dirty")]
    public void AndOneThatADerivedCellComputedWhenThatCellIsDirty()
    {
        var (graph, _) = Guarded("derived otherwise standby");

        Assert.Equal(5d, graph.Read("guarded"));

        // still caught after the cell it came from has been recomputed
        graph.Write("reading", 30d);
        graph.Step();

        Assert.Equal(5d, graph.Read("guarded"));
    }

    [Fact(DisplayName = "and it recovers, and stops depending on the fallback when it does")]
    public void AndItRecoversAndStopsDependingOnTheFallbackWhenItDoes()
    {
        // The dependency is the fallback's presence in the guarded cell, and it
        // has to come and go with the failure. While «failing» is an error the
        // fallback is an input; once it is a number the fallback is not read at
        // all, and writing to it must wake nothing.
        var (graph, evaluated) = Guarded("failing otherwise standby");

        Assert.Equal(5d, graph.Read("guarded"));
        Assert.Contains("standby", graph["guarded"].Dependencies);

        graph.Write("failing", 7d);
        graph.Step();

        Assert.Equal(7d, graph.Read("guarded"));
        Assert.DoesNotContain("standby", graph["guarded"].Dependencies);

        var before = evaluated();

        graph.Write("standby", 99d);
        graph.Step();

        Assert.Equal(7d, graph.Read("guarded"));
        Assert.Equal(before, evaluated());
    }

    [Fact(DisplayName = "a fault is not caught, and the fallback is not even asked")]
    public void AFaultIsNotCaughtAndTheFallbackIsNotEvenAsked()
    {
        // Found by audit, and it was one decision written down twice. A Fault IS
        // an Error, so "does this need a fallback" said yes while "does the
        // fallback win" said no — and the fallback was evaluated, and became an
        // input, of a cell no value of it could ever repair. Asserting the Fault
        // alone passes either way, which is why this asserts the dependency.
        var (graph, _) = Guarded("buggy otherwise standby");

        Assert.IsType<Fault>(graph.Read("guarded"));
        Assert.DoesNotContain("standby", graph["guarded"].Dependencies);
    }

    [Theory(DisplayName = "and it guards a call's result, not its last argument")]
    [InlineData("parse reading otherwise standby", "(parse «reading» otherwise «standby»)")]
    [InlineData("reading otherwise parse standby", "(«reading» otherwise parse «standby»)")]
    public void AndItGuardsACallsResultNotItsLastArgument(string source, string reading)
    {
        // Found by audit. A word pattern is available only where the requested
        // minimum is at most its own level, so an operator ABOVE that level takes
        // the call's last argument instead of its result — «parse input otherwise
        // standby» read as «parse («input» otherwise «standby»)», which supplies
        // a fallback to the argument and then calls with it. The mirror did not
        // resolve at all.
        //
        // The resolver's own summary of the pattern level says where this
        // belongs: under arithmetic, above the plumbing operators. A fallback is
        // plumbing.
        SymbolTable symbols = new();
        symbols.WithNames("reading", "standby").WithPatterns("parse _");

        Assert.Equal(reading, Of(symbols, source).Reading);
    }

    [Fact(DisplayName = "and the call's own failure is what it catches")]
    public void AndTheCallsOwnFailureIsWhatItCatches()
    {
        // The reading above is only half of it: this is the value, and it is
        // what the misreading silently got wrong. «parse» fails here, so the
        // fallback is the answer — under the old level the fallback went to the
        // argument, «parse» was called anyway, and its error was the result.
        SymbolTable symbols = new();
        symbols.WithNames("reading", "standby").WithPatterns("parse _");

        Assert.True(new Resolver(symbols).Resolve(Lexemes.Lex("parse reading otherwise standby"))
                                         .TryTree(out var tree));

        Scope scope = new();
        scope.Declare(new Declaration(Pattern.Parse("parse _"), [["text"]],
                                      (_, _) => new Error("«parse» could not read that")));

        Graph graph = new();
        graph.Var("reading", 20d);
        graph.Var("standby", 5d);
        graph.Let("guarded", cell => new Evaluator(scope).Evaluate(cell, tree, insideLet: true));
        graph.Prime();

        Assert.Equal(5d, graph.Read("guarded"));
    }

    /// <summary>
    ///     A «let» whose body is <paramref name="source"/>, and a count of how
    ///     many times it has been evaluated.
    /// </summary>
    private static (Graph Graph, Func<int> Recomputed) Guarded(string source)
    {
        SymbolTable symbols = new();
        symbols.WithNames("reading", "missing", "standby", "failing", "derived", "buggy");

        Assert.True(new Resolver(symbols).Resolve(Lexemes.Lex(source)).TryTree(out var tree), source);

        Scope scope = new();
        Evaluator evaluator = new(scope);
        var evaluated = 0;

        Graph graph = new();
        graph.Var("reading", 20d);
        graph.Var("missing", 0d);
        graph.Var("standby", 5d);

        // an error a cell HOLDS, and one a cell COMPUTES — the first is what
        // arms adoption on a plain read, and the second has to survive its own
        // recompute
        graph.Var("failing", new Error("bad input"));
        graph.Let("derived", cell => new Error($"no reading for {cell.Read("reading")}"));
        graph.Let("buggy", _ => throw new InvalidOperationException("defect"));

        graph.Let("guarded", cell =>
        {
            ++evaluated;
            return evaluator.Evaluate(cell, tree, insideLet: true);
        });

        graph.Prime();

        return (graph, () => evaluated);
    }
}
