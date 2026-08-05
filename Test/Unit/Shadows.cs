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

    [Fact(DisplayName = "a shadow is checked by R5 even when its source is not")]
    public void AShadowIsCheckedByR5EvenWhenItsSourceIsNot()
    {
        // A single-word name that IS a pattern's glue.
        var complaint = Assert.Single(Rules.Validate(
            [Declares("smoothed"), Declares("old smoothed", injectedBy: "smoothed")],
            [Shape("apply _ smoothed _")]));

        // The source name is caught, and the shadow's complaint is suppressed
        // because it would be the same mistake with the same fix.
        //
        // This used to be the other way round: R5 examined multi-word names
        // only, so «smoothed» passed and the collision was reachable ONLY
        // through «old smoothed» — which is why a separate injected-name finding
        // existed. Checking single-word names closed that gap, and the separate
        // finding went with it.
        var glue = Assert.IsType<GlueAsName>(complaint);

        Assert.Equal("smoothed", glue.Name);
    }

    [Fact(DisplayName = "one mistake reports once when both halves fail")]
    public void OneMistakeReportsOnceWhenBothHalvesFail()
    {
        // «hello to alice» and «old hello to alice» both contain the glue, but
        // there is one fix, so the shadow's complaint adds nothing
        var complaint = Assert.Single(Rules.Validate(
            [Declares("hello to alice"), Declares("old hello to alice", injectedBy: "hello to alice")],
            [Shape("send _ to _")]));

        Assert.Equal("hello to alice", Assert.IsType<GlueInName>(complaint).Name);
    }

    [Fact(DisplayName = "a name that only looks injected is an ordinary name")]
    public void ANameThatOnlyLooksInjectedIsAnOrdinaryName()
    {
        // WithNames is the raw scope and injects nothing, so «old growth rings»
        // with no «growth rings» beside it was written by someone rather than
        // generated — and it is theirs to rename, so it gets the ordinary
        // message.
        //
        // THREE words since R5′: «old growth» has its glue at the edge and is
        // admitted now, which is the narrowing working rather than a hole. That
        // also means an injected shadow can no longer reach this rule on its
        // own — every one of them is «old » and a name, so the glue it carries
        // is always at an edge.
        var complaint = Assert.Single(Rules.Validate([Declares("old growth rings")], [Shape("apply _ growth _")]));

        Assert.Equal("old growth rings", Assert.IsType<GlueInName>(complaint).Name);
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

    [Theory(DisplayName = "one pattern refines another only by adding words at a hole, and nothing else")]
    [InlineData("send _ to _", "send _ to all _", true)]
    [InlineData("send _ to _ now", "send _ to all _ later", false)]
    [InlineData("send _ to _", "send _ to all _ now", false)]
    [InlineData("send _ to _", "post _ to all _", false)]
    [InlineData("send _ to _", "send _ _ to _", false)]
    [InlineData("send _ to _", "send _ to all _ _", false)]
    [InlineData("send _ to _", "send _ to all now", false)]
    public void OnePatternRefinesAnotherOnlyByAddingWordsAtAHoleAndNothingElse(string shorter,
                                                                              string longer,
                                                                              bool refines)
    {
        // R7b's relation, one rejection at a time. Each row is a way two
        // patterns can look alike without one being the other plus a word at a
        // hole — a different TAIL, a longer tail, a different anchor, an
        // inserted HOLE rather than a word, and a longer form whose hole did not
        // survive the insertion.
        //
        // The last row is the one that needs its own guard: a hole LATER in the
        // inserted run leaves «all» first, so everything else lines up and the
        // relation would answer «all» for a pattern that is not «send (_) to
        // (_)» with a word added. A hole first answers null and is refused by
        // arithmetic; a hole second is not.
        //
        // Built directly rather than declared, because several of these are
        // shapes the grammar refuses on their own and the relation still has to
        // be right about them: it runs over whatever pair of patterns is in
        // scope, and being wrong here would refuse a name for a rivalry that
        // does not exist.
        var findings = Rules.Validate([Declares("all things"), Declares("things")],
                                      [Shape(shorter), Shape(longer)]);

        Assert.Equal(refines, findings.OfType<NameAbsorbsRefinement>().Any());
    }

    [Fact(DisplayName = "validating a scope derives the pattern relation once, not once per name")]
    public void ValidatingAScopeDerivesThePatternRelationOnceNotOncePerName()
    {
        // Found by audit. R7b recomputed the relation for every name against
        // every ordered pair of patterns — cubic in a scope, with LINQ slices
        // allocating on every comparison that failed. Fifty names and fifty
        // patterns took 360 ms and 140 MB to report nothing at all.
        //
        // ALLOCATION, and this is the second attempt. The first timed one run at
        // each size, and a wall clock inside a parallel suite measures the
        // machine's load as much as the algorithm — it failed once under an
        // ordinary run and passed in isolation, which is the worst way for a
        // gate to behave, because it teaches people to rerun.
        //
        // The signal is made deterministic by giving the derivation something to
        // allocate: two hundred patterns that all refine one shorter pattern, so
        // the relation is two hundred records rather than none. Deriving it per
        // name then multiplies THAT, and holding the patterns fixed while the
        // names grow is what isolates it.
        //
        //     derived once      282 KB -> 1,543 KB      5.5x
        //     derived per name  465 KB -> 6,655 KB     14.3x
        static long Work(int count)
        {
            Shape[] patterns = [Shape("send _ to _"),
                                .. Enumerable.Range(0, 200).Select(n => Shape($"send _ to w{n} _"))];

            Declared[] names = [.. Enumerable.Range(0, count).Select(n => Declares($"alpha{n} beta{n}"))];

            Rules.Validate(names, patterns).ToArray();

            var before = GC.GetAllocatedBytesForCurrentThread();

            Assert.Empty(Rules.Validate(names, patterns));

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        var one = Work(1);
        var many = Work(20);

        Assert.True(many < one * 9, $"one name allocated {one} bytes and twenty allocated {many}");
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
        // pattern already refused for an operator word went on reserving a
        // prefix through R7, and one refused for «old» went on reserving one
        // through R6b, each adding a finding whose repair the structural one
        // already states.
        var findings = Compilation.Of(new SourceText(source, "Player.ron")).Findings;

        Assert.Equal(only, Assert.Single(findings).GetType().Name);
    }

    [Fact(DisplayName = "and it stays one finding however many names are in scope")]
    public void AndItStaysOneFindingHoweverManyNamesAreInScope()
    {
        // The amplification is the reason, not the tidiness: every name
        // beginning «otherwise» collected its own copy, all with the same
        // repair — fix the pattern.
        var names = string.Concat(Enumerable.Range(0, 8).Select(n => $"var otherwise thing{n} => Number;\n"));

        var findings = Compilation.Of(new SourceText(
            names
          + "function send (x => Number) to (y => Number) { return x; }\n"
          + "function send (x => Number) to otherwise (y => Number) { return x; }\n",
            "Player.ron")).Findings;

        Assert.Equal(nameof(InfixInPattern), Assert.Single(findings).GetType().Name);
    }

    [Theory(DisplayName = "and a pinned hole reserves nothing, because no rival can reach it")]
    [InlineData(new int[] { 3 }, new int[] { 4 }, false)]
    [InlineData(new int[] { }, new int[] { }, true)]
    [InlineData(new int[] { }, new int[] { 4 }, true)]
    [InlineData(new int[] { 1 }, new int[] { }, false)]
    public void AndAPinnedHoleReservesNothingBecauseNoRivalCanReachIt(int[] shorter, int[] longer, bool refines)
    {
        // Found by audit. A pinned hole takes exactly one word or one bracketed
        // name, so it cannot swallow the multi-word name the rival reading
        // needs — «send x to all things» has no reading through «send (_) to
        // «_»», because the pin takes «all» and leaves «things» nowhere to go.
        // The relation compared spellings only, so it reserved «all» against an
        // ambiguity that cannot happen.
        //
        // The refined hole's own pin in the LONGER pattern is not compared:
        // what the rival needs is the shorter one being free, and both readings
        // exist whatever the longer does with the hole it kept. Row three.
        //
        // Built directly, because source has no pin syntax — the one built-in
        // pin is a first hole, which R7b skips — so the constructor is the only
        // way this shape is reachable, and it is reachable.
        Refinement[] found = [.. Rules.Refinements(
        [
            new Shape(new Pattern(["send", null, "to", null], shorter), default),
            new Shape(new Pattern(["send", null, "to", "all", null], longer), default),
        ])];

        Assert.Equal(refines, found.Length is not 0);
    }

    [Theory(DisplayName = "and a hole after the inserted words is compared where it moved to")]
    [InlineData(new int[] { 5 }, new int[] { 6 }, true)]
    [InlineData(new int[] { 5 }, new int[] { }, false)]
    public void AndAHoleAfterTheInsertedWordsIsComparedWhereItMovedTo(int[] shorter, int[] longer, bool refines)
    {
        // A hole BEFORE the insertion keeps its index and one after it gains the
        // run, so comparing pins needs to know which side of the hole it is on.
        // Every other row in these tests has its holes before the refined one,
        // where the two indices agree and an off-by-run would go unnoticed.
        //
        // «send (_) to (_) from (_)» refined at its second hole: the third hole
        // is at 5 in the shorter and 6 in the longer, so pinning 5 and 6 is the
        // same pattern and pinning it in one alone is not. The mismatch is
        // written as a missing pin rather than a moved one because the
        // constructor refuses a pin on a word, and index 5 of the longer is
        // «from» — so an off-by-run cannot even be spelled there.
        Refinement[] found = [.. Rules.Refinements(
        [
            new Shape(new Pattern(["send", null, "to", null, "from", null], shorter), default),
            new Shape(new Pattern(["send", null, "to", "all", null, "from", null], longer), default),
        ])];

        Assert.Equal(refines, found.Length is not 0);
    }
}
