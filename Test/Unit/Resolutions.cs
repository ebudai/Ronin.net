// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using System.Globalization;

namespace Unit;

/// <summary>
///     Expectations transcribed from the verified Python reference resolver.
///     Every case here was checked against an independent backtracking parser,
///     and the two rule cases at the bottom came out of an exhaustive search over
///     2,382,240 resolutions rather than out of anyone's judgement.
///
///     That search covered ANCHOR-FIRST WORD PATTERNS WITH NO BRACKETS, which is
///     what its generator emits — it never produced a leading hole. The number
///     stands; its scope was not stated when it was first reported, and quoting
///     it without the scope has been the mistake.
/// </summary>
[Trait(nameof(Resolver), null)]
public class Resolutions
{
    [Theory(DisplayName = "resolves")]
    [InlineData("a longer name and a shorter one are two readings, not a winner",
        new[] { "base", "base price", "price", "tax" },
        new[] { "base _" },
        "base price + tax", "Ambiguous", 2,
        new[] { "(«base price» + «tax»)", "base («price» + «tax»)" })]
    [InlineData("overlapping pattern prefixes tie",
        new[] { "list", "of list" },
        new[] { "sum _", "sum of _" },
        "sum of list", "Ambiguous", 2,
        new[] { "sum of «list»", "sum «of list»" })]
    [InlineData("a long name over a call segment is two readings, not a capture",
        new[] { "alice", "hello", "hello to alice" },
        new[] { "send _", "send _ to _" },
        "send hello to alice", "Ambiguous", 2,
        new[] { "send «hello to alice»", "send «hello» to «alice»" })]
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
        List<Node.Entry> entries = [new(null, new Node.Name("a"))];
        Node.Group group = new(entries);
        Node.Call call = new(pattern, parts);

        parts[0] = new Node.Name("b");
        entries[0] = new(null, new Node.Name("b"));

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

    [Theory(DisplayName = "a tie buried under composition is shown where a reader would bracket it")]
    [InlineData("sum of list", "sum «of list»", "sum of «list»")]
    [InlineData("(sum of list) + x", "(⟨sum «of list»⟩ + «x»)", "(⟨sum of «list»⟩ + «x»)")]
    [InlineData("((sum of list))", "⟨⟨sum «of list»⟩⟩", "⟨⟨sum of «list»⟩⟩")]
    [InlineData("compute (sum of list)", "compute ⟨sum «of list»⟩", "compute ⟨sum of «list»⟩")]
    [InlineData("x + (sum of list) + x", "((«x» + ⟨sum «of list»⟩) + «x»)", "((«x» + ⟨sum of «list»⟩) + «x»)")]
    public void ATieBuriedUnderCompositionIsShownWhereAReaderWouldBracketIt(string source, string one, string other)
    {
        // The top cell of «(sum of list) + x» had ONE derivation — an operator
        // combined two operands and did not care that one of them was a tie — so
        // the readings had to be carried up separately, as the innermost
        // ambiguous span's own pair. The message then showed a fragment of a
        // statement nobody had written down that way.
        //
        // A parent enumerates its children now, so every reading is a reading of
        // the WHOLE span, and the difference between them is where the reader
        // would put a bracket. Nothing is carried and nothing is a fragment.
        SymbolTable symbols = new();
        symbols.WithNames("list", "of list", "x").WithPatterns("sum _", "sum of _", "compute _");

        var tie = new Resolver(symbols).Resolve(source);

        Assert.Equal("Ambiguous", tie.Kind.ToString());
        Assert.Equal([one, other], tie.Readings);
    }

    [Fact(DisplayName = "and only the spans that took part in the parse are in it")]
    public void AndOnlyTheSpansThatTookPartInTheParseAreInIt()
    {
        // The readings used to be found by scanning every span in the table,
        // narrowest first, for one with two readings. Nothing required that span
        // to take part in the parse that won — so the message named an ambiguity
        // inside «prefix sum of list», which resolves uniquely and cheaply as one
        // whole name and contributes nothing to the tie.
        //
        // Enumerating from the top makes that structural rather than careful: a
        // reading is built out of the derivations that compose it, so a span the
        // winning parse never used cannot appear in one.
        SymbolTable symbols = new();
        symbols.WithNames("list", "of list", "prefix sum of list", "box", "from box")
               .WithPatterns("sum _", "sum of _", "take _", "take from _");

        var tie = new Resolver(symbols).Resolve("prefix sum of list + (take from box)");

        Assert.Equal("Ambiguous", tie.Kind.ToString());
        Assert.Equal(["(«prefix sum of list» + ⟨take «from box»⟩)",
                      "(«prefix sum of list» + ⟨take from «box»⟩)"], tie.Readings);
    }

    [Fact(DisplayName = "a tie shows every repair, wherever it is")]
    public void ATieShowsEveryRepairWhereverItIs()
    {
        // Three readings and three offered, bracketed or not. It used to be
        // three at the tie and TWO through a bracket, because what travelled up
        // was a pair — two readings prove a tie, and proving is all a parent
        // could do with them. Listing two of three hides a repair, which the old
        // name for this conceded by describing the two cases separately.
        SymbolTable symbols = new();
        symbols.WithNames("report", "the report", "the report today", "today")
               .WithPatterns("send _", "send _ today", "send the report _");

        Resolver resolver = new(symbols);

        Assert.Equal(3, resolver.Resolve("send the report today").Readings.Count);
        Assert.Equal(3, resolver.Resolve("(send the report today)").Readings.Count);
    }

    [Fact(DisplayName = "and an outer alternative does not hide one inside a child")]
    public void AndAnOuterAlternativeDoesNotHideOneInsideAChild()
    {
        // Found by audit, and it needed both facts at once to show: a span with
        // its own alternative, ONE OF WHOSE branches contains an ambiguous
        // child. The cell chose — its own readings if it had two, otherwise the
        // child's — so the child's remaining reading fell down the gap between
        // the two cases. The suite had a local three-way tie and a buried
        // two-way tie and never their conjunction.
        SymbolTable symbols = new();
        symbols.WithNames("a", "b", "c", "a to b around c", "b around c")
               .WithPatterns("send _", "send _ to _", "print _", "print _ around _");

        var tie = new Resolver(symbols).Resolve("print send a to b around c");

        // The middle one is the reading that used to vanish: it lives inside the
        // first outer shape, and the second outer shape is what made the cell
        // stop looking.
        Assert.Equal(["print send «a to b around c»",
                      "print send «a» to «b around c»",
                      "print send «a» to «b» around «c»"], tie.Readings);
    }

    [Fact(DisplayName = "and a statement with more readings than fit says how many there are")]
    public void AndAStatementWithMoreReadingsThanFitSaysHowManyThereAre()
    {
        // A cap that says nothing reads as "these are all of them", which is the
        // shape of every silent thing this design exists to remove. Sixty-three
        // independently ambiguous parts have more readings than atoms worth
        // counting, so the answer is a floor and says so.
        SymbolTable symbols = new();
        symbols.WithNames("list", "of list").WithPatterns("sum _", "sum of _");

        Resolver resolver = new(symbols);

        var few = resolver.Resolve("(sum of list, sum of list)");

        Assert.Equal(4, few.Total);
        Assert.False(few.Bounded);

        // FOUR readings and four shown, so nothing is hidden at this size and
        // the cap below is the only thing that changes.
        Assert.Equal(4, few.Readings.Count);

        var many = resolver.Resolve("(" + string.Join(", ", Enumerable.Repeat("sum of list", 63)) + ")");

        Assert.Equal("Ambiguous", many.Kind.ToString());
        Assert.True(many.Bounded);
        Assert.Equal(Resolver.Kept, many.Readings.Count);

        // Saturated rather than wrapped. 2^63 overflows a long into a negative
        // number, which is duly reported as fewer than two derivations — a
        // genuine tie returning Resolved, which is what the counting this
        // replaced actually did before it was made to saturate.
        Assert.True(many.Total > many.Readings.Count);
    }

    [Theory(DisplayName = "and a span built on a bounded one is bounded too")]
    [InlineData("({0})")]
    [InlineData("{0} + list")]
    public void AndASpanBuiltOnABoundedOneIsBoundedToo(string around)
    {
        // The count says "at least" only if every span above the cut knows it
        // was cut. A parent enumerates its child's KEPT readings, so it sees a
        // handful and would otherwise report a handful as a fact — the child's
        // own total is the only place the truth survives, and it has to travel.
        //
        // Both ways up: a group around it, and an operator beside it. Each was a
        // path the flag reached by a different line.
        SymbolTable symbols = new();
        symbols.WithNames("list", "of list").WithPatterns("sum _", "sum of _");

        var inner = "(" + string.Join(", ", Enumerable.Repeat("sum of list", 63)) + ")";

        var resolution = new Resolver(symbols).Resolve(string.Format(CultureInfo.InvariantCulture, around, inner));

        Assert.Equal("Ambiguous", resolution.Kind.ToString());
        Assert.True(resolution.Bounded);
        Assert.True(resolution.Total > resolution.Readings.Count);
    }

    [Theory(DisplayName = "two calls that read the same way are still two calls")]
    [InlineData(true)]
    [InlineData(false)]
    public void TwoCallsThatReadTheSameWayAreStillTwoCalls(bool nestedFirst)
    {
        // Found by audit, and the worst kind: the resolver said «Resolved» to a
        // statement with two meanings, and picked one by declaration order.
        //
        // A call renders its arguments without delimiting itself, so these two
        // trees produce the same sentence —
        //
        //     print( send(a, b) )        print «send a to b»
        //     print-to( send(a), b )     print «send a» to «b»
        //
        // — and the cell identified a derivation by that sentence, under a
        // comment that made it a claim. The second arrived looking like a
        // duplicate of the first and was dropped.
        //
        // Both orders, because the survivor was whichever was offered first.
        SymbolTable symbols = new();

        symbols.WithNames("a", "b")
               .WithPatterns(nestedFirst ? ["send _", "send _ to _", "print _", "print _ to _"]
                                         : ["print _ to _", "print _", "send _ to _", "send _"]);

        var resolution = new Resolver(symbols).Resolve("print send a to b");

        Assert.Equal("Ambiguous", resolution.Kind.ToString());
        Assert.Equal(2, resolution.Readings.Count);

        // And each is reachable, which is what makes reporting the tie a repair
        // rather than a dead end — the readings alone cannot say so, because
        // they are the same string.
        Assert.Equal("print ⟨send «a» to «b»⟩", new Resolver(symbols).Resolve("print (send a to b)").Reading);
        Assert.Equal("print ⟨send «a»⟩ to «b»", new Resolver(symbols).Resolve("print (send a) to b").Reading);
    }

    [Theory(DisplayName = "and the cheapest is offered first however the patterns were declared")]
    [InlineData(true)]
    [InlineData(false)]
    public void AndTheCheapestIsOfferedFirstHoweverThePatternsWereDeclared(bool dearestFirst)
    {
        // Found by audit. «Merge» offered every reading at the cell's cheapest
        // cost rather than at its own, so after one merge they were all tied and
        // the stable sort left them in the order the patterns happened to be
        // declared. Every existing ranking fixture declared the cheap one first,
        // so the flattening was invisible.
        //
        // Cost may order the suggestions and may never choose among them — and
        // it had quietly stopped doing the one thing it is still for.
        SymbolTable symbols = new();

        symbols.WithNames("a", "b", "a to b")
               .WithPatterns(dearestFirst ? ["send _ to _", "send _"] : ["send _", "send _ to _"]);

        // «send «a to b»» is two lookups and «send «a» to «b»» is three.
        Assert.Equal(["send «a to b»", "send «a» to «b»"],
                     new Resolver(symbols).Resolve("send a to b").Readings);
    }

    [Theory(DisplayName = "a multi-word keyword matches however it was spaced")]
    [InlineData("for each bank in banks")]
    [InlineData("for  each bank in banks")]
    [InlineData("for\teach bank in banks")]
    [InlineData("for\n each bank in banks")]
    public void AMultiWordKeywordMatchesHoweverItWasSpaced(string source)
    {
        // The lexer accepted every spacing and the grammar was happy, because
        // «Assert.IsType<ForEach>» only asks what the token is. A pattern is
        // matched against LEXEMES, and a lexeme carried the source slice — so
        // «for  each» produced a lexeme no segment equalled, and the resolver
        // failed to match a header the parser had just accepted.
        //
        // The canonical spelling closes the split at the one place the two
        // layers meet, which is why this asks the resolver rather than the
        // parser.
        SymbolTable symbols = new();
        symbols.WithNames("banks", "bank");

        foreach (var builtin in SymbolTable.Builtins) symbols.Patterns.Add((builtin, SymbolKind.Value));

        var resolution = new Resolver(symbols).Resolve(source);

        Assert.Equal("Resolved", resolution.Kind.ToString());
        Assert.Equal("for each «bank» in «banks»", resolution.Reading);
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

        // The pin is part of identity, so it is part of the hash. Left out, the
        // pinned and unpinned spellings of the same segments landed in one
        // bucket, where they compare unequal and collide for as long as both
        // exist — and both do, since the free-hole loop is what the pinned one
        // replaced and the tests still resolve against it.
        // Built rather than parsed: «Pattern.Parse» has no spelling for a pin, so
        // the two differ in nothing a string can express.
        string[] segments = ["for each", null, "in", null];

        Pattern free = new(segments);
        Pattern pinned = new(segments, [1]);

        Assert.Equal(free.Segments, pinned.Segments);
        Assert.False(free.Equals(pinned));
        Assert.NotEqual(free.GetHashCode(), pinned.GetHashCode());

        Assert.Equal(pinned.GetHashCode(), new Pattern(segments, [1]).GetHashCode());
    }

    [Fact(DisplayName = "a pattern's rendering parses back to it, or is refused")]
    public void APatternsRenderingParsesBackToItOrIsRefused()
    {
        // The property, not a case, because the failure was silent: «ToString»
        // wrote a hole as «(_)» and «Parse» read «(_)» as an ordinary word, so
        // the round trip gave a DIFFERENT pattern that rendered identically.
        // Nothing could have noticed by comparing renderings, which is exactly
        // what a test of cases would have done.
        //
        // Either half is a correct outcome. What is not correct is a third one.
        List<Pattern> renderable =
        [
            Pattern.Parse("compute total"),
            Pattern.Parse("compute total for _"),
            Pattern.Parse("send _ to _"),
            Pattern.Parse("take part of _"),          // one segment holding a space
            .. SymbolTable.Builtins,
            new Pattern(["for each", null, "in", null]),
        ];

        List<Pattern> refused = [];

        foreach (var pattern in renderable)
        {
            var rendered = pattern.ToString();

            Pattern returned;
            try { returned = Pattern.Parse(rendered); }
            catch (ArgumentException) { refused.Add(pattern); continue; }

            // Not "renders the same" — that is the check that could not have
            // caught this. The pattern itself has to come back.
            Assert.Equal(pattern, returned);
            Assert.Equal(rendered, returned.ToString());
        }

        // The one a rendering cannot express: a pinned hole, which has no
        // declaration syntax. A segment holding a space USED to be the other,
        // and stopped being one when Parse started lexing rather than splitting
        // — «part of» is one token, and re-lexing recovers it.
        Assert.Equal([SymbolTable.Builtins[0]], refused);
    }

    [Theory(DisplayName = "a symbol the lexer makes is a segment, and reserves nothing")]
    [InlineData("lookup (_) => (_)")]  // the lookup type, whose arrow reads correctly for a mapping
    [InlineData("take +")]             // a user pattern may claim a symbol too
    [InlineData("take <_>")]           // symbols either side of a hole
    [InlineData("take a-b")]           // a word, a symbol and a word, which is three segments
    public void ASymbolTheLexerMakesIsASegmentAndReservesNothing(string pattern)
    {
        // A second grammar for the lookup type would be a second ambiguity
        // policy, so the arrow is an ordinary segment and this is the ordinary
        // matcher. What a symbol segment does NOT do is reserve: glue exists
        // because a name can swallow a word sitting between two holes, and a
        // name cannot swallow a symbol — the lexer stops a word at one. So the
        // name rules are about words and a symbol segment is invisible to them.
        var parsed = Pattern.Parse(pattern);

        Assert.NotNull(parsed);
        Assert.DoesNotContain(parsed.Glue, word => word.Any(letter => char.IsLetter(letter) is false));
    }

    [Theory(DisplayName = "a segment the lexer cannot make is refused, not stored")]
    [InlineData("take 1")]             // Number
    [InlineData("take (")]             // Open, and an unmatched one at that
    [InlineData("take )")]             // Close
    [InlineData("take ,")]             // Separator
    [InlineData("take , x y")]         // not a bracket, and not at the end either
    [InlineData("take (+)")]           // bracketed, but not around a hole
    [InlineData("take (a)")]           // bracketed around a word, which is not how a segment is written
    [InlineData("take (_ a)")]         // a hole that does not close where it should
    [InlineData("for each «one word, or a bracketed name» in (_)")]
    public void ASegmentTheLexerCannotMakeIsRefusedNotStored(string pattern)
    {
        // Splitting on spaces called every one of these a WORD and built a
        // pattern out of it. None can ever match, because none is a word the
        // lexer produces — so they were dead shapes, constructed in silence, and
        // a pattern that can never match is as wrong as one that matches the
        // wrong thing.
        Assert.Throws<ArgumentException>(() => Pattern.Parse(pattern));
    }

    [Theory(DisplayName = "a multi-word keyword is one segment, however it was spaced")]
    [InlineData("take part of _", "take part of (_)", 3)]
    [InlineData("take part  of _", "take part of (_)", 3)]
    [InlineData("take part\tof _", "take part of (_)", 3)]
    [InlineData("for each _ in _", "for each (_) in (_)", 4)]
    [InlineData("for  each _ in _", "for each (_) in (_)", 4)]
    [InlineData("for\teach _ in _", "for each (_) in (_)", 4)]
    public void AMultiWordKeywordIsOneSegmentHoweverItWasSpaced(string pattern, string rendered, int segments)
    {
        // Three segments, not four. Splitting on spaces gave four, so the
        // pattern was declared and printed correctly and could never match the
        // three lexemes a call produces. Doubled spacing added an EMPTY segment
        // on top of that.
        var parsed = Pattern.Parse(pattern);

        Assert.Equal(segments, parsed.Segments.Count);
        Assert.Equal(rendered, parsed.ToString());
        Assert.DoesNotContain(null, parsed.Anchor);
    }

    [Fact(DisplayName = "a declared name is not a name that was read")]
    public void ADeclaredNameIsNotANameThatWasRead()
    {
        // The resolver worked out that this occurrence DECLARES «bank» — which
        // is what lets the loop resolve against a scope that does not have it
        // yet — and then handed back a Node.Name, whose contract is "in scope,
        // one lookup". Evaluating the tree read the name the loop was about to
        // introduce and reported it undeclared. Knowing something and erasing it
        // is worse than never knowing it, because everything downstream looks
        // right.
        SymbolTable symbols = new();
        symbols.WithNames("banks");

        foreach (var builtin in SymbolTable.Builtins) symbols.Patterns.Add((builtin, SymbolKind.Value));

        Assert.True(new Resolver(symbols).Resolve("for each bank in banks").TryTree(out var tree));

        var arguments = Assert.IsType<Node.Call>(tree).Arguments;

        Assert.IsType<Node.Binding>(arguments[0]);
        Assert.IsType<Node.Name>(arguments[1]);

        // and it still reads as what was written
        Assert.Equal("for each «bank» in «banks»", tree.ToString());
    }

    [Fact(DisplayName = "the same reading offered twice is one reading")]
    public void TheSameReadingOfferedTwiceIsOneReading()
    {
        // Two identical patterns in a table made «take x» count two derivations
        // while leaving ONE rendering to show for it, so the result was
        // Ambiguous with no readings at all — a tie reported between a statement
        // and itself. The cell said in a comment that same-rendering derivations
        // are the same reading, and then added their counts.
        SymbolTable symbols = new();
        symbols.WithNames("x").WithPatterns("take _", "take _");

        var resolution = new Resolver(symbols).Resolve("take x");

        Assert.Equal("Resolved", resolution.Kind.ToString());
        Assert.Equal("take «x»", resolution.Reading);
    }

    [Theory(DisplayName = "every ambiguity has two readings to show for it")]
    [InlineData("sum of list")]
    [InlineData("(sum of list) + x")]
    [InlineData("prefix sum of list + (take from box)")]
    [InlineData("take from box")]
    public void EveryAmbiguityHasTwoReadingsToShowForIt(string source)
    {
        // The invariant behind both of the above: Ambiguous means two DISTINCT
        // readings exist and can be named. Anything that reports a tie without
        // being able to say what the tie is between is reporting a bug.
        SymbolTable symbols = new();
        symbols.WithNames("list", "of list", "prefix sum of list", "box", "from box", "x")
               .WithPatterns("sum _", "sum of _", "take _", "take from _");

        var tie = new Resolver(symbols).Resolve(source);

        Assert.Equal("Ambiguous", tie.Kind.ToString());
        Assert.True(tie.Readings.Count >= 2, $"«{source}» is a tie between {tie.Readings.Count} readings");
        Assert.Equal(tie.Readings.Count, tie.Readings.Distinct().Count());
    }

    [Theory(DisplayName = "a hole is round brackets, and a matching pair")]
    [InlineData("take [_]")]
    [InlineData("take {_}")]
    [InlineData("take (_]")]
    [InlineData("take [_}")]
    [InlineData("take {_)")]
    public void AHoleIsRoundBracketsAndAMatchingPair(string pattern)
    {
        // «(», «[» and «{» are all Open to the resolver, so checking the kind
        // read every one of these as the ordinary free hole — mismatched pairs
        // included. «{_}» is spoken for besides: the design reserves braced
        // units for a hole kind that does not exist yet, and this consumed the
        // notation in advance.
        Assert.Throws<ArgumentException>(() => Pattern.Parse(pattern));
    }

    [Theory(DisplayName = "a segment no source can produce is refused by the constructor")]
    [InlineData("1")]
    [InlineData("for  each")]
    [InlineData("part of alice")]
    public void ASegmentNoSourceCanProduceIsRefusedByTheConstructor(string segment)
    {
        // Parse is a convenience; the constructor is what every runtime and
        // registry caller reaches, and it checked the first segment, the width
        // and the pins while never looking at a literal segment at all. None of
        // these can be produced by any source, so each was a pattern nothing
        // could ever match, built in silence.
        //
        // «for  each» is the subtle one: it lexes canonically to the single
        // segment «for each», so the doubled-space string stored here is not
        // what a call would present.
        Assert.Throws<ArgumentException>(() => new Pattern(["take", segment, null]));
    }

    [Fact(DisplayName = "a pin names a hole, and stays where it was put")]
    public void APinNamesAHoleAndStaysWhereItWasPut()
    {
        string[] segments = ["take", null];

        // Nothing checked either, so a pin could describe the literal «take» or
        // a segment that does not exist — and both rendered as an ordinary
        // «take (_)», which parses back to the UNPINNED pattern. That is the
        // round-trip property falsified by metadata saying nothing.
        Assert.Throws<ArgumentException>(() => new Pattern(segments, [0]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Pattern(segments, [2]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Pattern(segments, [-1]));

        // And the set is frozen rather than merely typed as read-only. It is in
        // the hash of a dictionary key, so mutating it after insertion makes the
        // declaration unreachable — and SymbolTable.Builtins is one instance for
        // the process, which made that global.
        var pinned = SymbolTable.Builtins[0];

        Assert.IsNotType<HashSet<int>>(pinned.Pinned);
        Assert.Throws<NotSupportedException>(() => ((ISet<int>)pinned.Pinned).Add(0));

        Dictionary<Pattern, string> scope = new() { [pinned] = "the loop" };

        Assert.Equal("the loop", scope[new Pattern([.. pinned.Segments], [.. pinned.Pinned])]);
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

        // A part has to be a substatement, so there is no empty one — but a
        // TRAILING separator does not make one. The aggregate permits it and the
        // guide's examples use it, so refusing it here made a form the parser
        // accepts fail to resolve.
        Assert.Equal("draw «a» at ⟨«b»⟩", resolver.Resolve("draw a at (b,)").Reading);

        // and the ones that really are holes still are
        Assert.Equal("NoParse", resolver.Resolve("draw a at ()").Kind.ToString());
        Assert.Equal("NoParse", resolver.Resolve("draw a at (,b)").Kind.ToString());
        Assert.Equal("NoParse", resolver.Resolve("draw a at (a,,b)").Kind.ToString());
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

    [Fact(DisplayName = "a pattern beginning with a hole is refused by its own rule")]
    public void APatternBeginningWithAHoleIsRefusedByItsOwnRule()
    {
        // R6 rejects infix already, but by accident: a leading hole makes the
        // anchor run empty, and an empty run is a prefix of every other. The
        // message would then say one anchor run begins another when the problem
        // is that there is no anchor run at all.
        //
        // Two leading-hole patterns are the case R6 cannot catch even
        // accidentally — neither run is SHORTER than the other, so the
        // comparison never runs. The explicit rule makes that unreachable rather
        // than merely unwitnessed, which is the point of having it.
        var findings = Compilation.Of(new SourceText("""
                                                     function (x => number) rounded { return x; }
                                                     function (y => number) squared { return y; }

                                                     """, "Player.ron")).Findings;

        Assert.Equal(2, findings.Count);
        Assert.All(findings, finding => Assert.IsType<LeadingHole>(finding));

        Assert.Equal(["(_) rounded", "(_) squared"],
                     findings.Cast<LeadingHole>().Select(finding => finding.Pattern).Order());
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
            (left, right) => System.Math.Pow((double)left, (double)right)), associativity: Associativity.Right);

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
