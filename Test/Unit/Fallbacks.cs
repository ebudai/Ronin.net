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

    /// <summary>
    ///     A «let» whose body is <paramref name="source"/>, and a count of how
    ///     many times it has been evaluated.
    /// </summary>
    private static (Graph Graph, Func<int> Recomputed) Guarded(string source)
    {
        SymbolTable symbols = new();
        symbols.WithNames("reading", "missing", "standby");

        Assert.True(new Resolver(symbols).Resolve(Lexemes.Lex(source)).TryTree(out var tree), source);

        Scope scope = new();
        Evaluator evaluator = new(scope);
        var evaluated = 0;

        Graph graph = new();
        graph.Var("reading", 20d);
        graph.Var("missing", 0d);
        graph.Var("standby", 5d);

        graph.Let("guarded", cell =>
        {
            ++evaluated;
            return evaluator.Evaluate(cell, tree, insideLet: true);
        });

        graph.Prime();

        return (graph, () => evaluated);
    }
}
