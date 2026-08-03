// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Runtime;

namespace Unit;

/// <summary>
///     «@» — indexing, one-based and closed.
/// </summary>
///
/// <remarks>
///     A symbol and not a word, because a word-spelled indexer puts its glue in
///     the reserved set and ends «RESERVED (0)». It already lexed — every
///     punctuation character is a one-character symbol — so what it was missing
///     was an entry in the one table that gives an operator its precedence and
///     its meaning together.
/// </remarks>
[Trait(nameof(Resolver), null)]
public class Indexing
{
    private static object Value(string source)
    {
        SymbolTable symbols = new();
        symbols.WithNames("list", "position");

        Assert.True(new Resolver(symbols).Resolve(Lexemes.Lex(source)).TryTree(out var tree), source);

        Graph graph = new();
        graph.Var("list", new object[] { 10d, 20d, 30d });
        graph.Var("position", 2d);

        return new Evaluator(new Scope()).Evaluate(graph, tree, insideLet: false);
    }

    /// <summary>
    ///     The same, from SOURCE — no hand-built array anywhere.
    /// </summary>
    ///
    /// <remarks>
    ///     Found by audit. Every test above supplies «new object[]» directly,
    ///     which proves the operator on data somebody built in C# and says
    ///     nothing about the spelling the language now has. «[10] @ 1» reported
    ///     that its left operand was not a list, and a two-element list happened
    ///     to work, so the shape of the fixture decided whether the feature did.
    /// </remarks>
    private static object Written(string source)
    {
        Assert.True(new Resolver(new SymbolTable()).Resolve(Lexemes.Lex(source)).TryTree(out var tree), source);

        return new Evaluator(new Scope()).Evaluate(new Graph(), tree, insideLet: false);
    }

    [Theory(DisplayName = "and a list written in source is a list at every size")]
    [InlineData("[ 10 ] @ 1", 10d)]
    [InlineData("[ 10, 20 ] @ 2", 20d)]
    [InlineData("[ 10, 20, 30 ] @ 3", 30d)]
    [InlineData("[ [ 1, 2 ], [ 3, 4 ] ] @ 1 @ 2", 2d)]
    public void AndAListWrittenInSourceIsAListAtEverySize(string source, double expected)
        => Assert.Equal(expected, Written(source));

    [Theory(DisplayName = "and a trailing comma is the aggregate's rule, not a hole")]
    [InlineData("[ 10, ] @ 1", 10d)]
    [InlineData("[ 10, 20, ] @ 2", 20d)]
    public void AndATrailingCommaIsTheAggregatesRuleNotAHole(string source, double expected)
        // Found by audit. The aggregate permits a trailing separator and the
        // guide's examples use it, so «[10, ]» compiles with no finding — and
        // then the resolver asked for an expression after the last comma, found
        // an empty span, and refused the whole group. A language-valid list that
        // would not resolve, and the cardinality tests missed it because none of
        // them was written in the form a person actually types.
        => Assert.Equal(expected, Written(source));

    [Theory(DisplayName = "and a leading or doubled one is still a hole")]
    [InlineData("[ , 10 ] @ 1")]
    [InlineData("[ 10, , 20 ] @ 1")]
    public void AndALeadingOrDoubledOneIsStillAHole(string source)
        => Assert.False(new Resolver(new SymbolTable()).Resolve(Lexemes.Lex(source)).TryTree(out _), source);

    [Fact(DisplayName = "and an empty one is a list with nothing in it, not a value")]
    public void AndAnEmptyOneIsAListWithNothingInItNotAValue()
    {
        // «[]» is the settled default for an empty square aggregate, and the
        // parser tried the lookup first, so it was one. Nothing downstream could
        // tell — a lookup and a list are the same brackets with nothing between
        // them — until an indexer asked.
        Assert.IsType<Error>(Written("[] @ 1"));

        // and a group is still a group: «(10)» is ten in brackets, not a list
        Assert.Equal(10d, Written("(10)"));
        Assert.IsType<Error>(Written("(10) @ 1"));
    }

    private static string Reading(string source)
    {
        SymbolTable symbols = new();
        symbols.WithNames("list", "a", "b", "c");

        return new Resolver(symbols).Resolve(source).Reading;
    }

    [Theory(DisplayName = "«@» binds tighter than arithmetic, and looser than nothing else")]
    [InlineData("list @ 4 + 1", "((«list» @ 4) + 1)")]
    [InlineData("a * b @ c", "(«a» * («b» @ «c»))")]
    [InlineData("a @ b @ c", "((«a» @ «b») @ «c»)")]
    public void BindsTighterThanArithmetic(string source, string reading)
        // What is indexed is the list beside it, not the sum — «list @ 4 + 1»
        // is the fifth element of nothing anyone wrote.
        => Assert.Equal(reading, Reading(source));

    [Theory(DisplayName = "and it counts from one")]
    [InlineData("list @ 1", 10d)]
    [InlineData("list @ 2", 20d)]
    [InlineData("list @ 3", 30d)]
    [InlineData("list @ position", 20d)]
    public void AndItCountsFromOne(string source, double expected) => Assert.Equal(expected, Value(source));

    [Fact(DisplayName = "and says so when someone counts from zero")]
    public void AndSaysSoWhenSomeoneCountsFromZero()
    {
        // Its own message and not the range's, because this is the mistake the
        // spelling exists to make unlikely and «0» is what someone arriving from
        // a zero-based language writes first.
        var refused = Assert.IsType<Error>(Value("list @ 0"));

        Assert.Contains("counts from one", refused.Message);
        Assert.Contains("«@ 1»", refused.Message);
    }

    [Theory(DisplayName = "and every other way of missing is a value, not a throw")]
    [InlineData("list @ 4", "no position 4")]
    [InlineData("list @ 2.5", "not one")]
    [InlineData("position @ 1", "indexes a list")]
    [InlineData("list @ list", "takes a number")]
    public void AndEveryOtherWayOfMissingIsAValueNotAThrow(string source, string says)
        // The reason division by zero is a value: an index past the end is an
        // ordinary thing for a program to compute, and the language already has
        // an answer for a computation that produced nothing useful.
        => Assert.Contains(says, Assert.IsType<Error>(Value(source)).Message);

    [Fact(DisplayName = "so a fallback reads as what it does")]
    public void SoAFallbackReadsAsWhatItDoes()
    {
        // The composition the whole spelling is for, and it needs «@» to bind
        // tighter than «otherwise» — which it does by twenty-one against six, so
        // the fallback guards the indexing rather than the position.
        Assert.Equal("((«list» @ 4) otherwise 0)", Reading("list @ 4 otherwise 0"));
        Assert.Equal(0d, Value("list @ 4 otherwise 0"));
        Assert.Equal(20d, Value("list @ 2 otherwise 0"));
    }

    [Fact(DisplayName = "a list that recomputes to the same list wakes nobody")]
    public void AListThatRecomputesToTheSameListWakesNobody()
    {
        // Found by audit. A list is a VALUE, so two lists with the same elements
        // are the same list — and .NET arrays compare by reference, so a
        // list-valued cell that rebuilt the same contents looked changed and
        // every reader below it woke on every tick.
        //
        // The count is the assertion, not the elements. Every reactive defect
        // this project has found was invisible in the value and visible in the
        // evaluation count, so a graph test asserting only on values measures
        // the one thing that has never been wrong.
        Graph graph = new();
        graph.Var("tick", 0d);

        graph.Let("list", scope => new object[] { 1d, scope.Read("tick") is double ? 2d : 2d });

        var downstream = 0;

        graph.Let("watching", scope =>
        {
            ++downstream;

            // Found by audit: this cast to «object[]» outlived the
            // representation, so the body faulted on its first evaluation and
            // the count below was counting a broken body's cutoff. It passed —
            // which is the point. Assert the element, so the count is taken on
            // the read the name describes.
            return ((IReadOnlyList<object>)scope.Read("list"))[0];
        });

        graph.Prime();

        Assert.Equal(1d, graph.Read("watching"));

        var settled = downstream;

        for (var tick = 1; tick <= 3; ++tick)
        {
            graph.Write("tick", tick);
            graph.Step();
            Assert.Equal(1d, graph.Read("watching"));
        }

        Assert.Equal(settled, downstream);
    }

    [Fact(DisplayName = "and a «changes» arm does not fire on a list that did not change")]
    public void AndAChangesArmDoesNotFireOnAListThatDidNotChange()
    {
        // Found by audit. Cutoff learned the language's equality and «changes»
        // did not, so a rebuilt-but-equal list was an edge — and the body of a
        // «changes» arm is an EFFECT, so it could write, create, remove or stop
        // on a change that did not happen.
        //
        // Testing «Builtin.Same» or the cutoff cannot show this: the question is
        // whether each consumer joined the one equality, and only counting the
        // body can answer it.
        Graph graph = new();
        graph.Var("tick", 0d);
        var fired = 0;

        // The CONDITION rebuilds the list, so «Trigger.Previous» is what compares
        // it. Reading a list-valued «let» instead cannot see this: recompute
        // cutoff already holds that node still, so nothing reaches the trigger
        // either way and the test passes however «changes» compares.
        graph.When("watching",
                   scope => new object[] { 1d, scope.Read("tick") is double ? 2d : 2d },
                   _ => ++fired,
                   TriggerMode.Changes);

        graph.Prime();

        var settled = fired;

        graph.Write("tick", 1d);
        graph.Step();

        Assert.Equal(settled, fired);
    }

    // The «old» half of the same repair has NO test here, deliberately. The
    // shadow comparison now uses the language's equality like every other
    // consumer, and I could not build a case that fails when it does not: a
    // shadow copies a cached value, recompute cutoff already holds a
    // list-valued «let» still, and every construction I tried passed under
    // reference equality too. A test that passes either way is the thing this
    // audit round keeps finding, so there is not one — the gap is recorded
    // instead of dressed up.

    [Fact(DisplayName = "a host's array is copied in, so mutating it changes nothing")]
    public void AHostsArrayIsCopiedInSoMutatingItChangesNothing()
    {
        // Found by audit. Lists were called immutable values and represented as
        // «object[]», handed straight in and straight back — so a caller could
        // change one without a graph write, nothing dirtied, and a direct read
        // and a derived read disagreed for ever. The same confidently-wrong
        // cache as a removed instance, admitted by the representation itself.
        var host = new object[] { 1d };

        Graph graph = new();
        graph.Var("xs", host);
        graph.Let("first", scope => Builtin.Operators["@"].Apply(scope.Read("xs"), 1d));
        graph.Prime();

        Assert.Equal(1d, graph.Read("first"));

        host[0] = 2d;

        // and the copy is DEEP: wrapping the caller's array would satisfy "the
        // storage is unreachable" and change nothing, because the caller still
        // holds it
        Assert.Equal(1d, graph.Read("first"));
        Assert.Equal(1d, ((List)graph.Read("xs"))[0]);
    }

    [Fact(DisplayName = "and one level down as well")]
    public void AndOneLevelDownAsWell()
    {
        // Shallow copying is the same defect one level down, and nested lists
        // are not exotic — they arrive with any grouped data.
        var inner = new object[] { 1d };

        Graph graph = new();
        graph.Var("xs", new object[] { inner });
        graph.Prime();

        inner[0] = 99d;

        Assert.Equal(1d, ((List)((List)graph.Read("xs"))[0])[0]);
    }

    [Fact(DisplayName = "and a list that contains itself is refused where it has a name")]
    public void AndAListThatContainsItselfIsRefusedWhereItHasAName()
    {
        // A cycle cannot be spelled in source, and the runtime API can build
        // one — so the guard is at the boundary, where the value still has a
        // caller and the message can say what is wrong. Inside a comparison the
        // two values are anonymous and the only honest answer is "too deep".
        var looping = new object[1];
        looping[0] = looping;

        Graph graph = new();
        graph.Var("xs", looping);

        Assert.Contains("cannot contain itself", Assert.IsType<Error>(graph.Read("xs")).Message);
    }

    [Fact(DisplayName = "and a value too deep to compare is refused rather than admitted and misjudged")]
    public void AndAValueTooDeepToCompareIsRefusedRatherThanAdmittedAndMisjudged()
    {
        // Found by audit, and it is the same test as before answering the
        // opposite way. The cap used to be in the comparison, which ACCEPTED a
        // 300-deep list and then said two equal ones differed — not an
        // equivalence, and visible wherever the runtime asks whether a value
        // changed: cutoff, «changes», «old», and «is» when it lands.
        //
        // So the limit moved to where the value is admitted. Everything that
        // reaches the comparison is now something it can answer about honestly.
        static object Nested(int levels)
        {
            object built = new object[] { 1d };

            for (var at = 0; at < levels; ++at) built = new object[] { built };

            return List.Of(built);
        }

        Assert.True(Builtin.Same(Nested(8), Nested(8)));
        Assert.True(Builtin.Same(Nested(List.Deep - 2), Nested(List.Deep - 2)));

        Assert.Contains("deeper", Assert.IsType<Error>(Nested(300)).Message);
    }

    [Fact(DisplayName = "and depth travels with the value, so wrapping cannot walk past the limit")]
    public void AndDepthTravelsWithTheValueSoWrappingCannotWalkPastTheLimit()
    {
        // A counter that starts at zero on every call is bypassed one layer at
        // a time: build a list at the limit, hand it back in, and the second
        // call counts from nothing. So the depth is carried by the value rather
        // than by the call, and the second wrap is refused on what it wraps.
        object built = new object[] { 1d };

        for (var at = 0; at < List.Deep - 1; ++at) built = new object[] { built };

        var admitted = Assert.IsType<List>(List.Of(built));

        Assert.Equal(List.Deep, admitted.Depth);
        Assert.Contains("deeper", Assert.IsType<Error>(List.Of(new object[] { admitted })).Message);
    }

    [Fact(DisplayName = "and an element that failed is an element, not the list's failure")]
    public void AndAnElementThatFailedIsAnElementNotTheListsFailure()
    {
        // Found by audit. The cycle guard reported itself as an «Error» — and an
        // error IS a value here — so the copy could not tell its own report from
        // an element it was copying, and any list containing a failed element
        // became that failure. Nobody asked for lifted construction; it arrived
        // as a side effect of a sentinel sharing a type with the thing it
        // travelled beside.
        Assert.Equal(2d, Value("[ 1 / 0, 2 ] @ 2"));

        var kept = Assert.IsType<List>(List.Of(new object[] { new Error("gone wrong"), 2d }));

        Assert.IsType<Error>(kept[0]);
        Assert.Equal(2d, kept[1]);
    }

    [Fact(DisplayName = "and a cycle beside a failed element is still a cycle")]
    public void AndACycleBesideAFailedElementIsStillACycle()
    {
        // The other half of the same confusion: with one type for both, an
        // error earlier in the value stopped the copy before it reached the
        // cycle, and the crash the guard exists to prevent came back.
        var looping = new object[2];
        looping[0] = new Error("gone wrong");
        looping[1] = looping;

        Assert.Contains("cannot contain itself", Assert.IsType<Error>(List.Of(looping)).Message);
    }

    [Fact(DisplayName = "and a list says what it is when something quotes it")]
    public void AndAListSaysWhatItIsWhenSomethingQuotesIt()
        // Diagnostics render values, so the value has to render as the language
        // spells it rather than as the host type behind it.
        => Assert.Equal("[1, 2]", Written("[ 1, 2 ]").ToString());

    [Fact(DisplayName = "and the empty list is one value, not one per mention")]
    public void AndTheEmptyListIsOneValueNotOnePerMention()
        // A singleton, which makes the commonest equality a reference check.
        // Not the intern table that was refused: no lookup, no growth, and
        // nothing to contend on.
        => Assert.Same(Written("[]"), Written("[ ]"));

    [Theory(DisplayName = "and equality is the language's, all the way down")]
    [InlineData("[ 1, 2 ]", "[ 1, 2 ]", true)]
    [InlineData("[ 1, 2 ]", "[ 2, 1 ]", false)]
    [InlineData("[ 1, 2 ]", "[ 1 ]", false)]
    [InlineData("[]", "[]", true)]
    [InlineData("[ [ 1 ], [ 2 ] ]", "[ [ 1 ], [ 2 ] ]", true)]
    [InlineData("[ [ 1 ], [ 2 ] ]", "[ [ 1 ], [ 3 ] ]", false)]
    public void AndEqualityIsTheLanguagesAllTheWayDown(string left, string right, bool same)
        // A list is order-sensitive, and nested lists compare by the same rule.
        // A LOOKUP would not — it is unordered, so two with the same
        // associations in a different order are the same lookup — and that is a
        // different function, unwritten because a lookup has no runtime value
        // yet: «[a = 1]» does not resolve.
        => Assert.Equal(same, Builtin.Same(Written(left), Written(right)));

    [Fact(DisplayName = "and a failure on either side is the answer")]
    public void AndAFailureOnEitherSideIsTheAnswer()
    {
        // Lifted, so an error arriving as either operand is what comes out
        // rather than being reported as a bad index.
        Assert.Equal("boom", Assert.IsType<Error>(
            Builtin.Operators["@"].Apply(new Error("boom"), 1d)).Message);

        Assert.Equal("boom", Assert.IsType<Error>(
            Builtin.Operators["@"].Apply(new object[] { 1d }, new Error("boom"))).Message);
    }
}
