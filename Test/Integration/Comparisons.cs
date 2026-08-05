// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Runtime;

namespace Integration;

/// <summary>
///     «is» — the language's equality, and the only one it has.
/// </summary>
///
/// <remarks>
///     <para>
///     ONE function, and it already existed. Cutoff, «changes», «old», pending
///     writes and member writes all ask <see cref="Builtin.Same"/> whether a
///     value moved, and that is the same question «is» asks — so registering it
///     could not introduce a second comparison to disagree with the first,
///     which is what a separate implementation would have been.
///     </para>
///     <para>
///     And no reference-equality partner, because for anything with identity
///     identity IS its equality: two boxes with equal members are two boxes, and
///     «is» on a handle says so by comparing handles. The operator that would
///     have meant "the same one" has nothing left to mean.
///     </para>
/// </remarks>
[Trait(nameof(Builtin), null)]
public class Comparisons
{
    private static Resolution Read(string source)
    {
        SymbolTable symbols = new();

        symbols.WithNames("a", "b", "c", "d", "total").WithPatterns("sum of _", "not _");

        var resolution = new Resolver(symbols).Resolve(Lexemes.Lex(source));

        // Asserted, not assumed. A reading is rendered whatever the kind is, so
        // a test comparing only the rendering passes just as happily on a tie
        // as on the answer.
        Assert.Equal("Resolved", resolution.Kind.ToString());

        return resolution;
    }

    /// <summary>
    ///     From source, through the resolver, to a value — the whole pipeline
    ///     rather than either end of it.
    /// </summary>
    ///
    /// <remarks>
    ///     Found by audit. The precedence rows asked the resolver and the
    ///     semantic rows called «Apply» on hand-built values, so neither half
    ///     proved that the operator the resolver SELECTS is the one that
    ///     evaluates. That is the same splice earlier rounds found at the
    ///     parser, declaration and runtime joins, and it is the join that would
    ///     break silently.
    /// </remarks>
    private static object Value(string source, params (string Name, object Value)[] world)
    {
        SymbolTable symbols = new();

        symbols.WithNames([.. world.Select(each => each.Name)]);

        Assert.True(new Resolver(symbols).Resolve(Lexemes.Lex(source)).TryTree(out var tree), source);

        Graph graph = new();

        foreach (var (name, value) in world) graph.Var(name, value);

        return new Evaluator(new Scope()).Evaluate(graph, tree, insideLet: false);
    }

    [Theory(DisplayName = "«is» binds looser than everything it has to")]
    [InlineData("a is b", "(«a» is «b»)")]
    [InlineData("a + b is c + d", "((«a» + «b») is («c» + «d»))")]
    [InlineData("a is total otherwise 0", "(«a» is («total» otherwise 0))")]
    [InlineData("sum of a is b", "(sum of «a» is «b»)")]
    public void IsBindsLooserThanEverythingItHasTo(string source, string reading)
    {
        // FIVE, and each row is a constraint that rules out a higher number.
        //
        // Below «PatternBindingPower» at 7, or «sum of a is b» reads as «sum of
        // (a is b)» — a trailing free hole parses its argument at the pattern's
        // own level, so the pattern swallows every comparison written after a
        // call.
        //
        // Below «otherwise» at 6, or «a is total otherwise 0» reads as «(a is
        // total) otherwise 0» — the fallback catching a truth, which can never
        // be nothing, when the thing that might be nothing is «total».
        //
        // Above arithmetic, or «a + b is c + d» compares «b» to «c» and adds the
        // answer to the rest.
        Assert.Equal(reading, Read(source).Reading);
    }

    [Theory(DisplayName = "and it groups to the left")]
    [InlineData("a is b is c", "((«a» is «b») is «c»)")]
    [InlineData("not a is b", "(not «a» is «b»)")]
    public void AndItGroupsToTheLeft(string source, string reading)
        // «not a is b» is «(not a) is b» and not what the English suggests,
        // because «not (_)» is a pattern at 7. That is the argument for «is not»
        // being its own form rather than a composition of the two, which is the
        // plan and is not this slice.
        => Assert.Equal(reading, Read(source).Reading);

    [Fact(DisplayName = "and chaining it compares a truth to a number, which nothing yet refuses")]
    public void AndChainingItComparesATruthToANumberWhichNothingYetRefuses()
    {
        // Found by audit, and the previous version of this test claimed the
        // opposite. It was called "where the type error goes" and said comparing
        // the intermediate truth with «c» is one — while asserting only the
        // resolver's brackets, so nothing ever asked any layer to produce the
        // error it promised.
        //
        // Run end to end it is «false»: «Same» hands a non-list pair to
        // «object.Equals», and true does not equal 1. That is a total,
        // heterogeneous equality, and it is the current behaviour whatever the
        // design eventually says.
        //
        // The DESIGN says type error, and there is no type layer to produce one
        // — «Evaluator.Apply» checks nothing before invoking an operator. So
        // this records what happens rather than what is intended, and the
        // intention is an unmet dependency rather than an established
        // invariant. When the type layer lands, this row is what has to change,
        // and it will fail rather than quietly keep agreeing.
        Assert.Equal(false, Value("a is b is c", ("a", 1d), ("b", 1d), ("c", 1d)));
    }

    [Theory(DisplayName = "and the operator the resolver picks is the one that evaluates")]
    [InlineData("a is b", true)]
    [InlineData("a is c", false)]
    public void AndTheOperatorTheResolverPicksIsTheOneThatEvaluates(string source, bool answer)
        // From source rather than from «Operators["is"]» by hand. Registering an
        // operator and reaching it are two things, and only one of them was
        // tested.
        => Assert.Equal(answer, Value(source, ("a", 1d), ("b", 1d), ("c", 2d)));

    [Fact(DisplayName = "and a failure read through it stays a failure")]
    public void AndAFailureReadThroughItStaysAFailure()
        // End to end, because «Lift» is invoked by the evaluator and asserting
        // it on the operator alone proves the operator, not the wiring.
        => Assert.IsType<Error>(Value("a is b", ("a", new Error("boom")), ("b", 1d)));

    [Fact(DisplayName = "and two instances are two instances, however alike")]
    public void AndTwoInstancesAreTwoInstancesHoweverAlike()
    {
        // The class's own claim, and it had no test: "two boxes with equal
        // members are two boxes". That sentence is the whole reason there is no
        // separate identity operator, so it is the one thing here that must not
        // be able to drift.
        //
        // Through «Graph» rather than by building «Instance» records, so this
        // guards the producer of identity as well as its consumer — a slot and
        // a generation are its parts, and a reused slot must not equal what held
        // it before.
        Graph graph = new();
        graph.Type("box", ("cash", 0d));

        var first = graph.Create("box");
        var second = graph.Create("box");

        var comparing = Builtin.Operators["is"];

        Assert.Equal(true, comparing.Apply(first, first));
        Assert.Equal(false, comparing.Apply(first, second));

        // Equal members, still two boxes.
        graph.Write("cash", first, 5d);
        graph.Write("cash", second, 5d);
        graph.Step();

        Assert.Equal(graph.Read("cash", first), graph.Read("cash", second));
        Assert.Equal(false, comparing.Apply(first, second));

        // And a slot that comes back is not what it held: the generation is
        // what makes a stale handle stale, so it has to be part of equality.
        graph.Remove(second);
        graph.Step();

        Assert.Equal(false, comparing.Apply(second, graph.Create("box")));
    }

    [Fact(DisplayName = "and it is the same comparison the graph already used")]
    public void AndItIsTheSameComparisonTheGraphAlreadyUsed()
    {
        // Two lists with equal elements are one value, so «is» says true — and
        // it says it through the function cutoff uses, which is what stops the
        // language shipping two answers to one question.
        Graph graph = new();
        graph.Var("xs", new object[] { 1d, 2d });
        graph.Var("ys", new object[] { 1d, 2d });
        graph.Var("zs", new object[] { 1d, 3d });

        var comparing = Builtin.Operators["is"];

        Assert.Equal(true, comparing.Apply(1d, 1d));
        Assert.Equal(false, comparing.Apply(1d, 2d));
        Assert.Equal(true, comparing.Apply(graph.Read("xs"), graph.Read("ys")));
        Assert.Equal(false, comparing.Apply(graph.Read("xs"), graph.Read("zs")));

        // Nothing is nothing. It is one value, so comparing it to itself is
        // true rather than the SQL answer.
        Assert.Equal(true, comparing.Apply(Nothing.Instance, Nothing.Instance));

        // And a failure is not compared. «otherwise» is the only operator that
        // inspects one; everything else inherits it, so asking whether a value
        // that failed equals anything answers with the failure.
        Assert.IsType<Error>(comparing.Apply(new Error("boom"), 1d));
        Assert.IsType<Error>(comparing.Apply(1d, new Error("boom")));
    }

    [Theory(DisplayName = "and a name may not span it, because no bracket selects one that does")]
    [InlineData("is valid", true)]
    [InlineData("valid is", true)]
    [InlineData("is", true)]
    [InlineData("y is x", false)]
    [InlineData("this is that thing", false)]
    public void AndANameMayNotSpanItBecauseNoBracketSelectsOneThatDoes(string name, bool legal)
    {
        // Nothing wired this up: «Rules.Infix» reads the operator table, so
        // registering «is» reserved it by the same derivation that reserved
        // «otherwise».
        //
        // And this is the half of R5′ that SURVIVED ambiguity becoming an error,
        // where the pattern-glue half did not. «a to b» reads only as itself, so
        // the ambiguity it causes is in some other statement and a bracket there
        // reaches it. «y is x» reads as a comparison of its own words — no
        // bracketing selects the name, so declaring it would be declaring
        // something unwriteable.
        //
        // Interior only, so «is valid» stays legal: an infix needs an operand on
        // each side, and a name that begins or ends with the word has nothing on
        // one side to compete with.
        var findings = Compilation.Of(new SourceText($"var {name} => Number;\n", "Player.ron")).Findings;

        if (legal)
        {
            Assert.Empty(findings);
            return;
        }

        Assert.Equal(nameof(InfixInName), Assert.Single(findings).GetType().Name);
    }

    [Fact(DisplayName = "and a name that spans it makes the statement ambiguous, not the name illegal")]
    public void AndANameThatSpansItMakesTheStatementAmbiguousNotTheNameIllegal()
    {
        // Where the refusal moved to. Declaring «y is x» is fine; writing it is
        // what has two readings, and that is reported where a reader can see
        // both and bracket one.
        SymbolTable symbols = new();

        symbols.WithNames("x", "y", "y is x");

        var resolution = new Resolver(symbols).Resolve(Lexemes.Lex("y is x"));

        Assert.Equal("Ambiguous", resolution.Kind.ToString());
        Assert.Equal(["«y is x»", "(«y» is «x»)"], resolution.Readings);
    }
}
