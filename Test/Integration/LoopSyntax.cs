// Copyright © 2026 Eric Budai

using Ronin.Compiler;

namespace Integration;

/// <summary>
///     «for each bank in banks», and the rule that makes it writable.
/// </summary>
///
/// <remarks>
///     <para>
///     The spelling was chosen in LOOPSYNTAX.md over «iterate banks =&gt; bank».
///     It is safe because the loop variable is PINNED to one word, so a header
///     has exactly one place to split and «in» stays an ordinary word. The first
///     way to get that guarantee was to reserve «in» against names; pinning gets
///     the same one without taking a word away from anyone. Without either, the
///     failure is not an ambiguity anyone would see — it is a strictly cheaper
///     wrong reading, and the resolver here reproduces it exactly as the design
///     note says:
///     </para>
///     <code>
///     name declared elsewhere   3 lookups   for each «order» in «transit in count of banks»
///     name absent               4 lookups   for each «order in transit» in count of «banks»
///     </code>
///     <para>
///     No tie, no error, a different program — and declaring an innocent variable
///     in another file is what switches between them. <see cref="TheHazardTheRuleExistsFor"/>
///     is the regression guard for the whole argument: if it ever resolves,
///     something has weakened R5.
///     </para>
/// </remarks>
[Trait(nameof(Compilation), null)]
public class LoopSyntax
{
    private static IReadOnlyList<Finding> Of(string source)
        => Compilation.Of(new SourceText(source, "Player.ron")).Findings;

    private static Resolution Resolve(string[] names, string source)
    {
        SymbolTable symbols = new();
        symbols.WithNames(names);

        foreach (var pattern in SymbolTable.Builtins) symbols.Patterns.Add(pattern);

        symbols.WithPatterns("count of _");

        return new Resolver(symbols).Resolve(source);
    }

    /// <remarks>
    ///     The names are the ENCLOSING scope's, and the loop variable is not
    ///     among them — because the loop is what declares it. Every one of these
    ///     used to supply it by hand, which is a table the real path can never
    ///     present: «Declarations.Bind» introduces the variable inside the body,
    ///     and reports shadowing if the enclosing scope already has it. So the
    ///     tests passed on a state that meant the valid loop returned NoParse
    ///     the moment anything real was handed to it.
    /// </remarks>
    [Theory(DisplayName = "a loop header has one reading")]
    // 1: the ordinary case
    [InlineData(new[] { "banks" }, "for each bank in banks", "for each «bank» in «banks»", 2)]
    // 2: a multi-word loop variable, which now takes brackets — see the pinned
    // hole. This is the entire bill for «in» costing nothing.
    [InlineData(new[] { "banks" }, "for each (open order) in banks", "for each «open order» in «banks»", 2)]
    // 3: the collection may itself be a pattern call
    [InlineData(new[] { "banks" }, "for each bank in count of banks", "for each «bank» in count of «banks»", 3)]
    // 4: and the variable's own name is nobody else's business, «in» included
    [InlineData(new[] { "banks" }, "for each in in banks", "for each «in» in «banks»", 2)]
    public void ALoopHeaderHasOneReading(string[] names, string source, string reading, int cost)
    {
        var resolution = Resolve(names, source);

        Assert.Equal("Resolved", resolution.Kind.ToString());
        Assert.Equal(reading, resolution.Reading);

        // The pattern and the collection, and nothing for the variable: it is a
        // BINDING occurrence, so there is nothing to look up — which is also why
        // it resolves without being declared.
        Assert.Equal(cost, resolution.Cost);
    }

    [Theory(DisplayName = "a binding hole takes a name and not a value")]
    [InlineData("for each (3) in banks")]                // a literal
    [InlineData("for each (a + b) in banks")]            // an operation
    [InlineData("for each (a, b) in banks")]             // several values
    [InlineData("for each [open order] in banks")]       // the wrong bracket
    [InlineData("for each {open order} in banks")]       // and the other wrong one
    [InlineData("for each (open order] in banks")]       // and one of each
    [InlineData("for each () in banks")]                 // and nothing at all to bind
    [InlineData("for each if in banks")]                 // a keyword may not lead a name
    [InlineData("for each while in banks")]
    [InlineData("for each part of in banks")]
    [InlineData("for each (if ready) in banks")]         // inside the brackets it is still a name
    public void ABindingHoleTakesANameAndNotAValue(string source)
    {
        // The parser's answer and the resolver's, as one matrix: whatever the
        // grammar refuses as a loop variable the resolver has to refuse too, or
        // joining them produces a program one of them never agreed to.
        Assert.NotEmpty(Of(source.Replace(" in banks", " in banks { return banks; }") + "\n"));

        // The pin fixes EXTENT, which is all the zero-glue argument needs — but
        // extent was being enforced by resolving the span as an ordinary
        // expression, and an expression is happy to be any of these. The grammar
        // accepts none of them, so the resolver accepted six shapes it could
        // never be handed and would not have known what to do with.
        //
        // Bracket kind included: the resolver files «(», «[» and «{» all as
        // Open, so nothing was checking that the brackets even matched.
        Assert.Equal("NoParse", Resolve(["banks", "a", "b", "open order"], source).Kind.ToString());
    }

    [Fact(DisplayName = "the hazard, and the pin that removes it")]
    public void TheHazardAndThePinThatRemovesIt()
    {
        // 9, and the argument's whole arc in one test.
        //
        // With a FREE hole the loop variable could grow leftward across an
        // earlier «in», and the competing readings did not tie — the capturing
        // one was strictly cheaper, so nothing reported it. That is why «in» had
        // to be reserved.
        var free = new Pattern(["for each", null, "in", null]);

        Assert.Equal(["in"], free.Glue);

        var captured = Resolve(free, ["order", "transit", "banks", "order in transit", "transit in count of banks"],
                               "for each order in transit in count of banks");
        var intended = Resolve(free, ["order", "transit", "banks", "order in transit"],
                               "for each order in transit in count of banks");

        Assert.NotEqual(intended.Reading, captured.Reading);
        Assert.True(captured.Cost < intended.Cost, "the capturing reading has to be the cheaper one");

        // PINNED, the hole is exactly one token, so there is one split point and
        // nothing to compete. The statement reads the way it is written whatever
        // anyone has declared, and «in» costs nothing.
        var pinned = SymbolTable.Builtins[0];

        Assert.Empty(pinned.Glue);

        foreach (var names in (string[][])
                 [
                     ["order", "transit", "banks", "order in transit", "transit in count of banks"],
                     ["order", "transit", "banks", "transit in count of banks"],
                 ])
        {
            var resolution = Resolve(pinned, names, "for each order in transit in count of banks");

            Assert.Equal("Resolved", resolution.Kind.ToString());
            Assert.Equal("for each «order» in «transit in count of banks»", resolution.Reading);
        }
    }

    private static Resolution Resolve(Pattern loop, string[] names, string source)
    {
        SymbolTable symbols = new();
        symbols.WithNames(names).WithPatterns("count of _");
        symbols.Patterns.Add(loop);

        return new Resolver(symbols).Resolve(source);
    }

    [Fact(DisplayName = "a pinned hole takes a word or a bracket and nothing else")]
    public void APinnedHoleTakesAWordOrABracketAndNothingElse()
    {
        // Determinate in EXTENT is the property, and a literal is neither a word
        // nor a bracketed group — it has no place a name could be declared.
        Assert.Equal("NoParse", Resolve(SymbolTable.Builtins[0], ["banks"], "for each 3 in banks").Kind.ToString());

        // and pinning works in trailing position too, where the hole has to be
        // the whole of what is left rather than merely start it
        var trailing = new Pattern(["take", null], [1]);

        Assert.Equal("Resolved", Resolve(trailing, ["bank", "banks"], "take bank").Kind.ToString());
        Assert.Equal("NoParse", Resolve(trailing, ["bank", "banks"], "take bank banks").Kind.ToString());
        Assert.Equal("Resolved", Resolve(trailing, ["open order"], "take (open order)").Kind.ToString());
    }

    [Fact(DisplayName = "«in» is not reserved at all")]
    public void InIsNotReservedAtAll()
    {
        // It was a lexer keyword, then a declaration rule, and now nothing. The
        // pinned hole makes the split structural, so there is no capture to
        // prevent and no legibility argument strong enough to charge every
        // program «in flight order», «logged in user» and «opt in list» for.
        Assert.Empty(Of("var in => number;\n"));
        Assert.Empty(Of("var in flight order => number;\n"));
        Assert.Empty(Of("var logged in user => number;\n"));

        // and the registry says so
        Assert.Empty(Glue.Reserved(SymbolTable.Builtins));
    }

    [Fact(DisplayName = "a single-word «in» never could capture anything")]
    public void ASingleWordInNeverCouldCaptureAnything()
    {
        // Why the reservation is legibility and not safety, checked rather than
        // assumed. Every one of these resolves uniquely with «in» declared as an
        // ordinary name, which is what makes it safe to enforce this at the
        // declaration where it can be scoped, typed, and later withdrawn.
        foreach (var source in (string[])
                 [
                     "for each bank in in",
                     "for each in in in",
                     "for each bank in count of in",
                 ])
        {
            Assert.Equal("Resolved", Resolve(["bank", "in", "banks"], source).Kind.ToString());
        }
    }

    [Fact(DisplayName = "a loop variable is a declaration, and is checked like one")]
    public void ALoopVariableIsADeclarationAndIsCheckedLikeOne()
    {
        // 5, and the item the design note calls easy to miss: a loop variable is
        // a declaration site, and if it skips the scope rules the whole argument
        // for this spelling collapses.
        //
        // Through R6b now. This used to be shown with pattern glue, and glue in
        // a name stopped being refused when ambiguity became the error — R6b is
        // the name rule that survived, because the readings it prevents are not
        // all reachable by bracketing.
        var findings = Of("""
                          function send (x => number) { return x; }
                          for each (send it) in orders { return it; }

                          """);

        var shadow = Assert.IsType<NameShadowsPattern>(
            Assert.Single(findings, finding => finding is NameShadowsPattern));

        Assert.Equal("send it", shadow.Name);
        Assert.Equal("send (_)", shadow.Pattern);

        // the span is on the variable inside the loop, not on the pattern it
        // would shadow
        Assert.Contains("for each ", shadow.Primary.Source.Text[..shadow.Primary.Offset]);
    }

    [Fact(DisplayName = "a loop variable that is fine is simply declared")]
    public void ALoopVariableThatIsFineIsSimplyDeclared()
    {
        // 6, from the other side: the variable is in scope inside the body and
        // nowhere else, so an outer name of the same spelling is a collision and
        // a sibling loop's is not.
        Assert.Empty(Of("""
                        for each bank in banks { return bank; }
                        for each bank in banks { return bank; }

                        """));

        var shadowed = Assert.IsType<Shadowed>(Assert.Single(Of("""
                                                                var bank => number;
                                                                for each bank in banks { return bank; }

                                                                """)));

        Assert.Equal("bank", shadowed.Name);
        Assert.Equal("in an enclosing scope", shadowed.Where);
    }

    [Fact(DisplayName = "«for (_)» is a legal user pattern")]
    public void ForIsALegalUserPattern()
    {
        // 8, accepted by the designer rather than merely diverged from. The
        // checklist demanded R6 reject «for (_)» beside «for each (_) in (_)»,
        // and the same document's own probe says why it need not:
        //
        //     "I went looking for a statement where «for (_)» could actually
        //      swallow a loop header, and there isn't one ... R6's rejection
        //      here is conservative, not load-bearing."
        //
        // loop_syntax.py §7 is that probe. Swallowing needs a name spanning
        // «... in ...», and GlueInName has already banned those — so the pair is
        // safe, and «for each» being one token means «for» is not an anchor
        // prefix of it under a model defined over lexer tokens.
        Assert.Empty(Of("function for (x => number) { return x; }\n"));

        // «for» is still an ordinary word, which is the point of spelling the
        // keyword «for each» — «compute total for (_)» is a pattern the language
        // wants and reserving «for» would have taken it away
        Assert.Empty(Of("function compute total for (order => number) { return order; }\n"));
    }

    [Fact(DisplayName = "a loop still parses to a loop, from source")]
    public void ALoopStillParsesToALoopFromSource()
    {
        var compilation = Compilation.Of(new SourceText("for each bank in banks { return bank; }\n", "Player.ron"));

        Assert.Empty(compilation.Findings);

        var loop = Assert.IsType<Ronin.Grammar.Scope.Iterating>(
            Assert.Single(compilation.Module.Scopes[0].Statements));

        Assert.Equal("bank", loop.Current.Words);
        Assert.NotNull(loop.Iterable);
    }
}
