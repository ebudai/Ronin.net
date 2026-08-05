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

        return new Resolver(symbols).Resolve(Lexemes.Lex(source));
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

    [Theory(DisplayName = "and it groups to the left, which is where the type error goes")]
    [InlineData("a is b is c", "((«a» is «b») is «c»)")]
    [InlineData("not a is b", "(not «a» is «b»)")]
    public void AndItGroupsToTheLeftWhichIsWhereTheTypeErrorGoes(string source, string reading)
        // «a is b is c» compares a truth to «c», which is a TYPE error rather
        // than a parse error — and that is the better place for it: "you
        // compared a truth to a number" beats "unexpected «is»".
        //
        // «not a is b» is «(not a) is b» and not what the English suggests,
        // because «not (_)» is a pattern at 7. That is the argument for «is not»
        // being its own form rather than a composition of the two, which is the
        // plan and is not this slice.
        => Assert.Equal(reading, Read(source).Reading);

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

    [Theory(DisplayName = "and reserving it costs the names it was measured to cost")]
    [InlineData("is valid", true)]
    [InlineData("valid is", true)]
    [InlineData("is", true)]
    [InlineData("y is x", false)]
    [InlineData("this is that thing", false)]
    public void AndReservingItCostsTheNamesItWasMeasuredToCost(string name, bool legal)
    {
        // Nothing wired this up: «Rules.Infix» reads the operator table, so
        // registering «is» reserved it in the same commit and by the same
        // derivation that reserved «otherwise».
        //
        // And R5′ is what makes the bill affordable. «is» is the sixth commonest
        // word in identifiers, and the blanket rule would have refused «is
        // valid» — the canonical boolean-name shape, and the one a
        // spaces-in-names grammar most encourages. Interior only, so what is
        // refused is what a reader would also misparse: «y is x» reads as a
        // comparison and «is valid» does not.
        var findings = Compilation.Of(new SourceText($"var {name} => Number;\n", "Player.ron")).Findings;

        if (legal)
        {
            Assert.Empty(findings);
            return;
        }

        Assert.Equal(nameof(InfixInName), Assert.Single(findings).GetType().Name);
    }
}
