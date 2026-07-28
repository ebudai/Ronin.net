// Copyright © 2026 Eric Budai

using Ronin.Compiler;

namespace Unit;

/// <summary>
///     Expectations transcribed from the verified Python reference resolver.
///     Every case here was checked against an independent backtracking parser,
///     and the two rule cases at the bottom came out of an exhaustive search over
///     2,382,240 resolutions rather than out of anyone's judgement.
/// </summary>
[Trait(nameof(Resolver), null)]
public class Resolutions
{
    [Theory(DisplayName = "resolves")]
    [InlineData("maximal munch beats splitting a name",
        new[] { "base", "base price", "price", "tax" },
        new[] { "base _" },
        "base price + tax", "Resolved", 2,
        new[] { "(«base price» + «tax»)" })]
    [InlineData("overlapping pattern prefixes tie",
        new[] { "list", "of list" },
        new[] { "sum _", "sum of _" },
        "sum of list", "Ambiguous", 2,
        new[] { "sum of «list»", "sum «of list»" })]
    [InlineData("a long name swallows a call segment",
        new[] { "alice", "hello", "hello to alice" },
        new[] { "send _", "send _ to _" },
        "send hello to alice", "Resolved", 2,
        new[] { "send «hello to alice»" })]
    [InlineData("control: no swallowing name in scope",
        new[] { "alice", "hello" },
        new[] { "send _", "send _ to _" },
        "send hello to alice", "Resolved", 3,
        new[] { "send «hello» to «alice»" })]
    [InlineData("nested pattern calls",
        new[] { "list" },
        new[] { "print _", "sum of _" },
        "print sum of sum of list", "Resolved", 4,
        new[] { "print sum of sum of «list»" })]
    [InlineData("pattern glue inside a name ties",
        new[] { "order", "total", "total for order" },
        new[] { "compute _", "compute total for _" },
        "compute total for order", "Ambiguous", 2,
        new[] { "compute total for «order»", "compute «total for order»" })]
    [InlineData("three-way overlap",
        new[] { "report", "the report", "the report today", "today" },
        new[] { "send _", "send _ today", "send the report _" },
        "send the report today", "Ambiguous", 2,
        new[] { "send the report «today»", "send «the report today»", "send «the report» today" })]
    [InlineData("medial argument crosses an operator",
        new[] { "a", "b", "c" },
        new[] { "send _ to _" },
        "send a + b to c", "Resolved", 4,
        new[] { "send («a» + «b») to «c»" })]
    [InlineData("trailing argument absorbs a tighter operator",
        new[] { "a", "b" },
        new[] { "compute total for _" },
        "compute total for a + b", "Resolved", 3,
        new[] { "compute total for («a» + «b»)" })]
    [InlineData("bracketing the parameter does not stop the extent",
        new[] { "a", "b" },
        new[] { "compute total for _" },
        "compute total for (a) + b", "Resolved", 4,
        new[] { "compute total for (⟨«a»⟩ + «b»)" })]
    [InlineData("bracketing the call does stop it",
        new[] { "a", "b" },
        new[] { "compute total for _" },
        "(compute total for a) + b", "Resolved", 4,
        new[] { "(⟨compute total for «a»⟩ + «b»)" })]
    [InlineData("operator precedence under a pattern",
        new[] { "a", "b", "c" },
        new[] { "print _" },
        "print a + b * c", "Resolved", 4,
        new[] { "print («a» + («b» * «c»))" })]
    // The transcribed corpus had two more cases here, «a looser operator stays
    // outside» and «pipeline is not swallowed». Both turned on an operator binding
    // looser than a pattern call, and the only two such operators — «<>» at 5 and
    // «|>» at 3 — are not part of the language. See OpenCallCannotBeATightOperand
    // for what still exercises PatternBindingPower.
    [InlineData("literals cost nothing",
        new[] { "a" },
        new[] { "print _" },
        "print 42", "Resolved", 1,
        new[] { "print 42" })]
    [InlineData("unknown word does not parse",
        new[] { "a" },
        new[] { "print _" },
        "print bogus thing", "NoParse", 0,
        new string[] { })]
    // `kind` is a string, not the ResolutionKind enum: the enum is internal to
    // Ronin.Compiler, and a public xunit method may not expose a less accessible
    // type in its signature (CS0051).
    public void Resolves(string _, string[] names, string[] patterns, string source,
                         string kind, int cost, string[] readings)
    {
        SymbolTable symbols = new();
        symbols.WithNames(names).WithPatterns(patterns);

        Resolver resolver = new(symbols);
        var resolution = resolver.Resolve(source);

        Assert.Equal(kind, resolution.Kind.ToString());
        if (kind is "NoParse") return;

        Assert.Equal(cost, resolution.Cost);
        Assert.Equal(readings.OrderBy(r => r, StringComparer.Ordinal),
                     resolution.Readings.OrderBy(r => r, StringComparer.Ordinal));
    }

    [Fact(DisplayName = "an open call cannot be a tight operand")]
    public void OpenCallCannotBeATightOperand()
    {
        // What is left of PatternBindingPower once «<>» and «|>» are gone. Every
        // remaining operator binds tighter than a pattern call, so the constraint
        // is only observable in this direction: a call ending in an unbracketed
        // trailing argument returns at PatternBindingPower and so cannot be the
        // operand of an operator demanding more.
        SymbolTable symbols = new();
        symbols.WithNames("data", "x").WithPatterns("sum of _");

        Resolver resolver = new(symbols);

        // the call itself is fine, so the failure below is the binding power and
        // not a call that never resolved
        Assert.Equal("Resolved", resolver.Resolve("sum of x").Kind.ToString());

        Assert.Equal("NoParse", resolver.Resolve("data + sum of x").Kind.ToString());

        // bracketing closes the call, and a closed atom has no binding power to
        // violate — the same repair that resolves a tie
        Assert.Equal("Resolved", resolver.Resolve("data + (sum of x)").Kind.ToString());
    }

    [Fact(DisplayName = "bracketing repairs every tie")]
    public void BracketingRepairsEveryTie()
    {
        // The ambiguity message tells the writer to bracket, so the edit it
        // proposes has to actually resolve the case: a fix suggestion that does
        // not work is worse than none. Every ambiguous case in the corpus is
        // repaired here, and the repairs must reach as many distinct readings as
        // there were competitors — otherwise the language has programs nobody
        // can write.
        Repairs(["list", "of list"], ["sum of _", "sum _"],
                "sum of list",
                ["sum of (list)", "sum (of list)"]);

        Repairs(["order", "total", "total for order"], ["compute _", "compute total for _"],
                "compute total for order",
                ["compute total for (order)", "compute (total for order)"]);

        Repairs(["report", "the report", "the report today", "today"],
                ["send _", "send _ today", "send the report _"],
                "send the report today",
                ["send (the report today)", "send (the report) today", "send the report (today)"]);
    }

    [Fact(DisplayName = "a pattern owns its segments, and a tree owns its children")]
    public void APatternOwnsItsSegmentsAndATreeOwnsItsChildren()
    {
        // Identity IS the segment sequence, and a runtime scope is keyed on it —
        // so keeping the caller's list meant mutating that list changed the hash
        // of a live key. The declaration became unreachable BOTH by the pattern
        // that made it and by a freshly built equal one: not moved, stranded.
        List<string> segments = ["compute", null];
        Pattern pattern = new(segments);
        Dictionary<Pattern, string> scope = new() { [pattern] = "here" };

        segments[0] = "reckon";

        Assert.True(scope.ContainsKey(pattern));
        Assert.True(scope.ContainsKey(new Pattern(["compute", null])));
        Assert.Equal("compute (_)", pattern.ToString());

        // and the same for the nodes, which cache their rendering — a caller
        // still holding the list could change what a node contains without
        // changing what it says it contains
        List<Node> parts = [new Node.Name("a")];
        Node.Group group = new(parts);
        Node.Call call = new(pattern, parts);

        parts[0] = new Node.Name("b");

        Assert.Equal("⟨«a»⟩", group.ToString());
        Assert.Equal("compute «a»", call.ToString());
    }

    [Fact(DisplayName = "a group of ties is still a tie, however many of them there are")]
    public void AGroupOfTiesIsStillATieHoweverManyOfThemThereAre()
    {
        // Match and Expression saturated every multiplication; Group saturated
        // once, at the end, over a raw product across its parts. Sixty-three
        // independently ambiguous parts reached 2^63, which wraps to negative,
        // reads as fewer than two derivations, and returns a genuine tie as
        // Resolved — a statement with sixty-three ambiguities each silently
        // decided.
        SymbolTable symbols = new();
        symbols.WithNames("list", "of list").WithPatterns("sum _", "sum of _");

        Resolver resolver = new(symbols);

        // 63 is where it wrapped, and one is the control. The span between them
        // costs seconds rather than milliseconds, which is finding 17 showing
        // through: the table is cubic in the token count and allocated eagerly.
        foreach (var parts in (int[])[1, 63])
        {
            var group = "(" + string.Join(", ", Enumerable.Repeat("sum of list", parts)) + ")";

            Assert.Equal("Ambiguous", resolver.Resolve(group).Kind.ToString());
        }
    }

    private static void Repairs(string[] names, string[] patterns, string ambiguous, string[] repairs)
    {
        SymbolTable symbols = new();
        symbols.WithNames(names).WithPatterns(patterns);

        Resolver resolver = new(symbols);

        var tie = resolver.Resolve(ambiguous);
        Assert.Equal("Ambiguous", tie.Kind.ToString());

        HashSet<string> reached = [];
        foreach (var repair in repairs)
        {
            var resolution = resolver.Resolve(repair);
            Assert.Equal("Resolved", resolution.Kind.ToString());
            reached.Add(resolution.Reading);
        }

        Assert.Equal(tie.Readings.Count, reached.Count);
    }

    [Theory(DisplayName = "anchor and glue decompose a pattern")]
    [InlineData("apply _ smoothed _", "apply", "smoothed")]
    [InlineData("compute total for _", "compute total for", "")]
    [InlineData("send _ to _", "send", "to")]
    [InlineData("sum of _", "sum of", "")]
    public void AnchorAndGlueDecomposeAPattern(string source, string anchor, string glue)
    {
        // The two scope rules read off this split and neither is obvious by
        // eye: R6 compares anchors, R5 reserves glue. A design note once claimed
        // «compute total for (_)» made «for» glue — it does not, because every
        // word of it precedes the hole — and the example reached a test, where
        // it passed vacuously, since an example that cannot fire cannot fail.
        var pattern = Pattern.Parse(source);

        Assert.Equal(anchor, string.Join(' ', pattern.Anchor));
        Assert.Equal(glue, string.Join(' ', pattern.Glue));
    }

    [Fact(DisplayName = "a pattern is its segments, not its rendering")]
    public void APatternIsItsSegmentsNotItsRendering()
    {
        // Identity has to be structural, or a scope keyed on patterns collides
        // silently the first time the rendering changes for presentation.
        var first = Pattern.Parse("compute total for _");
        var same = Pattern.Parse("compute total for _");
        var different = Pattern.Parse("compute total of _");

        Assert.True(first.Equals(same));
        Assert.False(first.Equals(different));
        Assert.False(first.Equals((Pattern)null));
        Assert.False(first.Equals(first.ToString()));

        Assert.Equal(first.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(first.GetHashCode(), different.GetHashCode());
    }

    [Fact(DisplayName = "a group holds one part or several")]
    public void AGroupHoldsOnePartOrSeveral()
    {
        SymbolTable symbols = new();
        symbols.WithNames("a", "b").WithPatterns("draw _ at _");

        Resolver resolver = new(symbols);

        Assert.Equal("draw «a» at ⟨«b»⟩", resolver.Resolve("draw a at (b)").Reading);
        Assert.Equal("draw «a» at ⟨«a», «b»⟩", resolver.Resolve("draw a at (a, b)").Reading);

        // the bracket is one lookup either way, so the second costs exactly the
        // extra name it contains
        Assert.Equal(4, resolver.Resolve("draw a at (b)").Cost);
        Assert.Equal(5, resolver.Resolve("draw a at (a, b)").Cost);

        // a separator inside a nested bracket belongs to the inner group and must
        // not split the outer one
        Assert.Equal("draw «a» at ⟨⟨«a», «b»⟩, «b»⟩", resolver.Resolve("draw a at ((a, b), b)").Reading);
        Assert.Equal(7, resolver.Resolve("draw a at ((a, b), b)").Cost);

        // a part has to be a substatement, so there is no empty one
        Assert.Equal("NoParse", resolver.Resolve("draw a at (b,)").Kind.ToString());
        Assert.Equal("NoParse", resolver.Resolve("draw a at ()").Kind.ToString());
    }

    [Theory(DisplayName = "bracket cost is ranking neutral")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(9)]
    public void BracketCostIsRankingNeutral(int _)
    {
        // Brackets are explicit tokens, so every parse of a token stream contains
        // the same number of them and the cost shifts all readings equally.
        // Verified across the corpus at costs 0, 1 and 9: zero verdict changes.
        SymbolTable symbols = new();
        symbols.WithNames("a", "b").WithPatterns("compute total for _");

        Resolver resolver = new(symbols);
        Assert.Equal("Resolved", resolver.Resolve("compute total for (a + b)").Kind.ToString());
        Assert.Equal("Resolved", resolver.Resolve("(compute total for a) + b").Kind.ToString());
    }


    /// <summary>
    ///     Spans for a rule test, which reads symbols and never their positions.
    ///     Rendering from real findings is what the golden file covers.
    /// </summary>
    private static readonly SourceText Nowhere = new(string.Empty);

    private static Declared Declares(string name, string injectedBy = null)
        => new(name, Nowhere.Span(0, 0), injectedBy);

    private static Ronin.Compiler.Shape Shape(string pattern) => new(Pattern.Parse(pattern), Nowhere.Span(0, 0));

    [Fact(DisplayName = "anchor runs must be prefix free")]
    public void AnchorRunsMustBePrefixFree()
    {
        // Found by exhaustive search: «b (_)» and «b b (_)» tie on «b b b a» with
        // no name involved, so no naming rule can repair it.
        var complaint = Assert.Single(Rules.Validate([], [Shape("b _"), Shape("b b _")]));

        var anchors = Assert.IsType<AnchorPrefix>(complaint);

        Assert.Equal("b (_)", anchors.Prefix);
        Assert.Equal("b b (_)", anchors.Pattern);
    }

    [Fact(DisplayName = "names may not contain pattern glue")]
    public void NamesMayNotContainPatternGlue()
    {
        // Without this, defining «hello to alice» silently re-resolves
        // «send hello to alice» from a two-argument call to a one-argument one.
        var complaint = Assert.Single(Rules.Validate([Declares("hello to alice")], [Shape("send _ to _")]));

        Assert.Equal(FindingKind.GlueInName, complaint.Kind);
        var glue = Assert.IsType<GlueInName>(complaint);

        Assert.Equal("hello to alice", glue.Name);
        Assert.Equal("to", glue.Word);
        Assert.Equal("send (_) to (_)", glue.Pattern);
    }

    [Fact(DisplayName = "operators of one precedence chain")]
    public void OperatorsOfOnePrecedenceChain()
    {
        // Regression: both operands were being parsed at BindingPower + 1, which
        // forbids the operator on either side, so «a + b + c» did not parse at all.
        SymbolTable symbols = new();
        symbols.WithNames("a", "b", "c");

        Resolver resolver = new(symbols);

        Assert.Equal("((«a» + «b») + «c»)", resolver.Resolve("a + b + c").Reading);
        Assert.Equal("((«a» * «b») / «c»)", resolver.Resolve("a * b / c").Reading);

        // and precedence still wins over grouping
        Assert.Equal("((«a» + («b» * «c»)) - «a»)", resolver.Resolve("a + b * c - a").Reading);
    }

    [Fact(DisplayName = "a right associative operator nests to the right")]
    public void ARightAssociativeOperatorNestsToTheRight()
    {
        // Nothing in the language is right associative yet. Operator carries the
        // flag, so the mirror of the rule above is worth pinning before something
        // is: left takes the higher minimum, right takes the operator's own.
        SymbolTable symbols = new();
        symbols.WithNames("a", "b", "c");
        symbols.Operators["^"] = new Operator(25, Ronin.Runtime.Builtin.Lift(
            (left, right) => System.Math.Pow((double)left, (double)right)), IsLeftAssociative: false);

        Resolver resolver = new(symbols);

        Assert.Equal("(«a» ^ («b» ^ «c»))", resolver.Resolve("a ^ b ^ c").Reading);
    }

    [Fact(DisplayName = "a resolution that found nothing reads as nothing")]
    public void AResolutionThatFoundNothingReadsAsNothing()
    {
        SymbolTable symbols = new();
        Resolver resolver = new(symbols);

        Assert.Equal(string.Empty, resolver.Resolve("bogus").Reading);
        Assert.Equal(string.Empty, Resolution.NoParse.Reading);
    }

    [Fact(DisplayName = "the boundaries are the lexer's, not a second opinion")]
    public void TheBoundariesAreTheLexersNotASecondOpinion()
    {
        // What the splitter used to be asked, asked of the real lexer. It agreed
        // on three of these and diverged on the last two — «7.» was one lexeme
        // to it and a number followed by a point to the lexer, and «_x1» was one
        // word to it and two lexemes to the lexer. Every resolver expectation ran
        // through the splitter, so those two were places where the tests and the
        // compiler disagreed and nothing said so.
        Assert.Equal(["3.5", "<=", "x"], Text("3.5 <= x"));
        Assert.Equal(["a", "<=", "(", "b", ")"], Text("a<=(b)"));
        Assert.Equal(["a", "+"], Text("a +"));

        // a lone point after a number is not part of it
        Assert.Equal(["7", "."], Text("7."));

        // and an underscore is not a word character
        Assert.Equal(["_", "x1"], Text("_x1"));
    }

    private static string[] Text(string source) => [.. Lexemes.Lex(source).Select(lexeme => lexeme.Text)];

    [Fact(DisplayName = "a resolution describes itself")]
    public void AResolutionDescribesItself()
    {
        // ToString is what a programmer reads when a statement will not resolve, so
        // the ambiguous case has to name every competing reading — that list is the
        // whole repair instruction.
        SymbolTable symbols = new();
        symbols.WithNames("list", "of list").WithPatterns("sum of _", "sum _");

        Resolver resolver = new(symbols);

        Assert.Equal("no parse", resolver.Resolve("bogus").ToString());
        Assert.Equal("1 lookup(s): «list»", resolver.Resolve("list").ToString());

        var ambiguous = resolver.Resolve("sum of list").ToString();
        Assert.StartsWith("ambiguous at 2 lookup(s)", ambiguous);
        Assert.Contains("sum of «list»", ambiguous);
        Assert.Contains("sum «of list»", ambiguous);
    }

    [Fact(DisplayName = "the resolver rejects a nonsense configuration")]
    public void TheResolverRejectsANonsenseConfiguration()
    {
        SymbolTable symbols = new();

        Assert.Throws<ArgumentNullException>(() => new Resolver(null));

        // the table indexes minimum binding power from 0 to MaxBindingPower, so a
        // pattern outside that range would index off the end of it
        Assert.Throws<ArgumentOutOfRangeException>(() => new Resolver(symbols, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Resolver(symbols, 31));

        Resolver resolver = new(symbols);
        Assert.Throws<ArgumentNullException>(() => resolver.Resolve((IReadOnlyList<Lexeme>)null));
        Assert.Equal("NoParse", resolver.Resolve(Array.Empty<Lexeme>()).Kind.ToString());
    }

    [Fact(DisplayName = "a pattern must have segments")]
    public void APatternMustHaveSegments()
    {
        Assert.Throws<ArgumentNullException>(() => new Pattern(null));
        Assert.Throws<ArgumentException>(() => Pattern.Parse(string.Empty));
    }

    [Fact(DisplayName = "a word pattern may not begin with a hole")]
    public void WordPatternMayNotBeginWithHole()
    {
        // Left recursive: resolving an atom at p would require an atom at p.
        // Infix must be symbolic; word patterns must be prefix.
        Assert.Throws<ArgumentException>(() => Pattern.Parse("_ plus _"));
    }
}
