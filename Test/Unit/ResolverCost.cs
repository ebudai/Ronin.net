// Copyright © 2026 Eric Budai

using Ronin.Compiler;

namespace Unit;

/// <summary>
///     What resolving a statement costs, held to a ceiling.
/// </summary>
///
/// <remarks>
///     <para>
///     The table is cubic in the token count — two <c>(n+1)²</c> tables and one
///     <c>(n+1)² × levels</c> — so the cost of getting this wrong is not
///     proportional to the mistake. Resolving a statement of 299 lexemes
///     allocated 766 MB, and two changes took it to 126 MB:
///     </para>
///     <list type="bullet">
///         <item>
///             index only the minimum binding powers the recurrences can ask
///             for, which is six and not thirty-two — and derive them from the
///             operator table, so an operator added at a new level cannot index a
///             slot that is not there
///         </item>
///         <item>allocate a cell's collections on first offer rather than at construction</item>
///         <item>
///             store spans triangularly: a span runs from «i» to «j» with
///             i &lt;= j, so half of a rectangular table is spans that cannot
///             exist and the largest table paid for that half once per binding
///             power
///         </item>
///         <item>
///             index patterns by their first word, so a span asks only the
///             patterns that could begin at it rather than all of them
///         </item>
///     </list>
///     <para>
///     A ceiling rather than a benchmark: this is a regression test, and the
///     number sits well above what it costs now and well below what it cost
///     before, so it fails on a return to the old shape and not on ordinary
///     variation. What remains is a pooled table for repeated editor calls, which
///     is a lifetime question rather than a shape one.
///     </para>
/// </remarks>
[Trait(nameof(Resolver), null)]
public class ResolverCost
{
    /// <summary>
    ///     The resolver's allocation budget, in megabytes.
    /// </summary>
    ///
    /// <remarks>
    ///     ONE constant, used by the comparison and by the message. They were two
    ///     numbers and the raise moved only one, so a regression between them
    ///     would have failed while quoting a limit nothing enforced.
    /// </remarks>
    private const int Ceiling = 32;

    [Fact(DisplayName = "resolving stays within its allocation budget")]
    public void ResolvingStaysWithinItsAllocationBudget()
    {
        SymbolTable symbols = new();
        symbols.WithNames("base price", "tax").WithPatterns("compute total for _", "send _ to _");

        Resolver resolver = new(symbols);

        var lexemes = Lexemes.Lex(string.Join(" + ", Enumerable.Repeat("base price", 50)));

        Assert.Equal(149, lexemes.Count);

        // the first call JITs and warms; the measurement is of the second
        Assert.Equal("Resolved", resolver.Resolve(lexemes).Kind.ToString());

        var before = GC.GetAllocatedBytesForCurrentThread();
        resolver.Resolve(lexemes);
        var megabytes = (GC.GetAllocatedBytesForCurrentThread() - before) / 1024.0 / 1024.0;

        // 26.2 MB as this is written, and «is» is what moved it — 15 with two
        // operator precedences, 21.7 with three, 26.2 with four. The table
        // carries a column per level the recurrences can ask for, so a level
        // costs about 4.5 MB and an operator that borrows an existing one costs
        // nothing: six cost exactly what nine did. A language cannot have many
        // more operators than it needs, and this is where that shows up rather
        // than in a benchmark nobody runs.
        //
        // It was 158 MB before the binding-power and lazy-collection work, and
        // 22 before the table went triangular, which scales to about 37 at four
        // levels. So 32 still fails on losing any of the three, and keeps about
        // the margin 26 kept over 21.7 — 22% against 20%.
        //
        // WHAT IS COMING, so the budget is known before it is spent: «and» and
        // «or» are reserved at 1 to 4 and are looser than comparison, so they
        // want their own levels. One more is about 31 MB and two are about 35.
        // The next operator has to move this number again and say what it did to
        // the margin, as this one has.
        Assert.True(megabytes < Ceiling,
                    $"resolving 149 lexemes allocated {megabytes:F1} MB, past the {Ceiling} MB ceiling");
    }

    [Fact(DisplayName = "a statement past the ceiling is refused, not resolved slowly")]
    public void AStatementPastTheCeilingIsRefusedNotResolvedSlowly()
    {
        // Cubic in the lexeme count, so one generated or pasted statement can ask
        // for arbitrarily much of the table. Per-statement resolution bounds the
        // ordinary case and not that one.
        SymbolTable symbols = new();
        symbols.WithNames("a");

        Resolver resolver = new(symbols);

        // n names with n-1 operators between them, so 2n-1 lexemes — which
        // straddles the ceiling rather than landing on it
        var within = Lexemes.Lex(string.Join(" + ", Enumerable.Repeat("a", Resolver.MaxLexemes / 2)));
        var past = Lexemes.Lex(string.Join(" + ", Enumerable.Repeat("a", (Resolver.MaxLexemes / 2) + 1)));

        Assert.Equal(Resolver.MaxLexemes - 1, within.Count);
        Assert.Equal(Resolver.MaxLexemes + 1, past.Count);

        Assert.Equal("Resolved", resolver.Resolve(within).Kind.ToString());

        var refused = resolver.Resolve(past);

        // distinct from a failure to parse: the statement may be perfectly good
        // and nothing here found out
        Assert.Equal("TooLong", refused.Kind.ToString());
        Assert.Contains("split it", refused.ToString());
    }

    [Fact(DisplayName = "a pattern has a width, because matching recurses over it")]
    public void APatternHasAWidthBecauseMatchingRecursesOverIt()
    {
        // Match recurses one frame per segment, and nothing else bounds that: a
        // pattern's width comes from a declaration, which the statement ceiling
        // does not constrain.
        var widest = Pattern.Parse(string.Join(' ', Enumerable.Repeat("word", Pattern.MaxSegments)));

        Assert.Equal(Pattern.MaxSegments, widest.Segments.Count);

        var wider = string.Join(' ', Enumerable.Repeat("word", Pattern.MaxSegments + 1));

        var refused = Assert.Throws<ArgumentException>(() => Pattern.Parse(wider));

        Assert.Contains("at most", refused.Message);
    }

    [Fact(DisplayName = "an operator is refused where it is written, not where it is used")]
    public void AnOperatorIsRefusedWhereItIsWrittenNotWhereItIsUsed()
    {
        // The table is mutable so a scope can add an operator, and every invalid
        // entry failed far from the insertion that caused it: a binding power
        // outside the indexed range came back as a raw IndexOutOfRangeException
        // while CONSTRUCTING a resolver, and a null implementation resolved
        // perfectly well and then threw inside the evaluator.
        object apply(object left, object right) => left;

        foreach (var power in (int[])[-1, Resolver.MaxBindingPower + 1, int.MaxValue])
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Operator(power, apply));
        }

        Assert.Throws<ArgumentNullException>(() => new Operator(10, null));

        // and the range's own edges are legal
        Assert.Equal(0, new Operator(0, apply).BindingPower);
        Assert.Equal(Resolver.MaxBindingPower, new Operator(Resolver.MaxBindingPower, apply).BindingPower);
    }

    [Fact(DisplayName = "only the binding powers something asks for get a slot")]
    public void OnlyTheBindingPowersSomethingAsksForGetASlot()
    {
        // Zero, the pattern binding power, and each operator's own power and one
        // above it. «E[i, j, 13]» is reachable only if an operator binds at 13,
        // and the table carried it regardless.
        //
        // The list is the operator table's, so an operator added at a new level
        // shows up here — «is» at 5 did — and the cost of that level shows up in
        // the budget above.
        SymbolTable symbols = new();

        Assert.Equal([5, 6, 10, 20, 21], symbols.Operators.Values.Select(op => op.BindingPower).Distinct().Order());

        // An operator added at a new level has to widen the table with it.
        // Hard-coding six would leave every statement using the new operator
        // silently unresolvable, which is why the resolver reads the operator
        // table rather than a constant — so this asserts the derivation, not the
        // number.
        symbols.Operators["^"] = new Operator(25, Ronin.Runtime.Builtin.Lift(
            (left, right) => System.Math.Pow((double)left, (double)right)), isLeftAssociative: false);
        symbols.WithNames("a", "b", "c");

        Resolver added = new(symbols);

        Assert.Equal("Resolved", added.Resolve("a + b").Kind.ToString());

        // right associative, so «a ^ b ^ c» groups to the right — which is the
        // half of the recurrence that needs the «power + 1» slot
        Assert.Equal("(«a» ^ («b» ^ «c»))", added.Resolve("a ^ b ^ c").Reading);
    }
    [Fact(DisplayName = "a witness is made once and kept as it rises")]
    public void AWitnessIsMadeOnceAndKeptAsItRises()
    {
        // Found by audit. The comments said the resolver's producers made the
        // owned value once so a «Best» would keep it, and no producer did — they
        // built ordinary collections, so every non-empty witness was copied at
        // «Best», and one travelling up through brackets was copied again at
        // each level it rose through.
        //
        //     sum of list            25,976  ->  25,720
        //     (sum of list)          39,720  ->  39,248
        //     ((((sum of list))))    96,424  ->  95,304
        //
        // IDENTITY and not the byte count. The saving is about one percent, so a
        // ceiling wide enough not to be brittle would not notice losing it,
        // where "the same object came out" fails the moment a producer stops
        // owning what it builds. The numbers are here because they are why.
        //
        // BOTH producers, now that the other one has somewhere to be tested
        // from. Saying it could not be discriminated was true of the factoring
        // and not of the code: the cell's tie was one expression inside a
        // private nested type, so reverting it left every test green — «Best»
        // owns whatever it is handed and repairs both cases before anything
        // downstream can tell them apart.
        var made = Best.Pair(new List<string> { "one", "two", "three" });

        Assert.Same(made, Owned.Copy(made));
        Assert.Same(made, Best.Pair(made));
        Assert.Same(made, Best.Either([], made));
        Assert.Same(made, new Best(1, null, 2, made).Witness);

        // The other producer, asserted where it produces rather than after
        // «Best» has normalised it. A 112-byte difference per tie is not
        // something a ceiling can see, and this is exact.
        //
        // The cell that CALLS it is no longer this test's job. Its tie branch is
        // declared «Owned.Kept», and an ordinary collection expression cannot
        // satisfy that type — the mutation that used to pass here now fails to
        // compile, which is a better place for it than an assertion.
        //
        // The remaining rewrite — building an ordinary collection inside the
        // producer and copying it — is guarded separately below, and saying it
        // could not be was wrong for the third time in the same way: it was a
        // fact about the arrangement, not about the problem. Extracting the
        // producer is what made it measurable, and that had already happened.
        var tied = Best.Readings([new Node.Name("one"), new Node.Name("two")]);

        Assert.Same(tied, Owned.Copy(tied));
        Assert.Equal(["«one»", "«two»"], tied);
    }
    [Fact(DisplayName = "and each producer builds its value once, in the cheapest shape that owns it")]
    public void AndEachProducerBuildsItsValueOnceInTheCheapestShapeThatOwnsIt()
    {
        // Found by audit twice over. «Owned.Kept» proves the value it hands back
        // owns its storage; it cannot prove nothing was built on the way there,
        // and three shapes all satisfy it:
        //
        //     Readings   mapped factory              64 bytes per call
        //                «Select» into «Owned.Of»   112
        //                ordinary collection, copy  224
        //
        //     Pair       the two values              64
        //                a collection holding them  128
        //                «Take(2)»                  152
        //
        // I compared two of each and picked the better, which is how a choice
        // between two looks settled while the answer is neither. The baseline
        // here is now the CHEAPEST shape rather than whichever one production
        // happens to use — against the middle form as oracle, a regression to it
        // would have passed.
        //
        // Two measurements in one process, so it is machine-independent, and a
        // ratio rather than a number so unrelated movement does not edit it.
        static long Work(Func<object> make)
        {
            for (var at = 0; at < 200; ++at) make();

            var before = GC.GetAllocatedBytesForCurrentThread();

            for (var at = 0; at < 1_000; ++at) make();

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        Node[] order = [new Node.Name("one"), new Node.Name("two")];
        var witness = Owned.Copy<string>(["one", "two", "three"]);

        // ONE: each producer against the factory it calls. This catches a
        // rewrite in the producer — «Owned.Copy([.. …])», «Select», «Take(2)» —
        // because the factory stays where it is while the producer moves.
        var mapped = Work(() => Owned.Of(order, static node => node.ToString()));
        var elements = Work(() => Owned.Of(witness[0], witness[1]));

        Assert.True(Work(() => Best.Readings(order)) * 2 < mapped * 3,
                    $"the mapped factory allocates {mapped} bytes and «Best.Readings» does not match it");

        Assert.True(Work(() => Best.Pair(witness)) * 2 < elements * 3,
                    $"the element factory allocates {elements} bytes and «Best.Pair» does not match it");

        // TWO: each factory against something that does not call it. Found by
        // audit — the assertions above compare a producer with the very factory
        // it invokes, so a factory that got slower moved both sides together and
        // the ratio never noticed. They guard delegation, which is worth
        // guarding, and they cannot guard shape.
        //
        // «Barely» is the shape the answer has to be: one array of the right
        // size, filled, and one object holding it. Nothing in the compiler is
        // reached to measure it.
        //
        //     a bare array and wrapper   64 bytes per call
        //     the factory               64
        //     the producer              64
        //
        // Every layer at the floor, so the bound has its whole margin against
        // the shapes that fail it — 112 for the iterator and 128 for the
        // intermediate collection.
        Assert.True(mapped * 2 < Work(() => Barely.Mapping(order)) * 3,
                    $"a bare array and wrapper allocates {Work(() => Barely.Mapping(order))} bytes and the mapped factory does not match it");

        Assert.True(elements * 2 < Work(() => Barely.Two(witness[0], witness[1])) * 3,
                    $"a bare array and wrapper allocates {Work(() => Barely.Two(witness[0], witness[1]))} bytes and the element factory does not match it");
    }

    /// <summary>
    ///     The least a list of these values can cost: one array, one object.
    /// </summary>
    ///
    /// <remarks>
    ///     A test-only oracle, and it exists because the obvious baseline was
    ///     the implementation. Comparing «Owned.Of» with «Owned.Of» measures
    ///     nothing about how «Owned.Of» is written.
    /// </remarks>
    private sealed class Barely(string[] values)
    {
        public static Barely Two(string first, string second) => new([first, second]);

        public static Barely Mapping(IReadOnlyList<Node> order)
        {
            var made = new string[order.Count];

            for (var at = 0; at < made.Length; ++at) made[at] = order[at].ToString();

            return new(made);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public override string ToString() => string.Join(", ", values);
    }
}
