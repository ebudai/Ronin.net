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
///     It is safe because a multi-word name may not contain «in», so a loop
///     header has exactly one and there is exactly one place to split it. Without
///     that rule the failure is not an ambiguity anyone would see — it is a
///     strictly cheaper wrong reading, and the resolver here reproduces it
///     exactly as the design note says:
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

    [Theory(DisplayName = "a loop header has one reading")]
    // 1: the ordinary case
    [InlineData(new[] { "bank", "banks" }, "for each bank in banks", "for each «bank» in «banks»")]
    // 2: a multi-word loop variable is fine as long as it has no «in» in it
    [InlineData(new[] { "open order", "banks" }, "for each open order in banks", "for each «open order» in «banks»")]
    // 3: the collection may itself be a pattern call
    [InlineData(new[] { "bank", "banks" }, "for each bank in count of banks", "for each «bank» in count of «banks»")]
    public void ALoopHeaderHasOneReading(string[] names, string source, string reading)
    {
        var resolution = Resolve(names, source);

        Assert.Equal("Resolved", resolution.Kind.ToString());
        Assert.Equal(reading, resolution.Reading);
    }

    [Fact(DisplayName = "the hazard the rule exists for")]
    public void TheHazardTheRuleExistsFor()
    {
        // 9. With both names in scope the wrong reading is CHEAPER, so nothing
        // flags it — the same shape as «send hello to alice», which is what put
        // R5 in the language. With R5 enforced neither name can exist, and the
        // statement has no reading at all, which is correct: it was never
        // writable.
        const string source = "for each order in transit in count of banks";

        var captured = Resolve(["order", "transit", "banks", "order in transit", "transit in count of banks"], source);
        var intended = Resolve(["order", "transit", "banks", "order in transit"], source);

        Assert.Equal("Resolved", captured.Kind.ToString());
        Assert.Equal("Resolved", intended.Kind.ToString());

        Assert.NotEqual(intended.Reading, captured.Reading);
        Assert.True(captured.Cost < intended.Cost, "the capturing reading has to be the cheaper one");

        // and with the rule doing its job, neither name is declarable
        Assert.Equal("NoParse", Resolve(["order", "transit", "banks"], source).Kind.ToString());
    }

    [Fact(DisplayName = "«in» is reserved at the declaration, not in the lexer")]
    public void InIsReservedAtTheDeclarationNotInTheLexer()
    {
        // 4 and 7. «in» was a keyword briefly, which reserved it in the lexer:
        // unconditionally, untypeably, unscopeably, and permanently. It is a
        // declaration rule now, and that is not a smaller hammer but the right
        // one — the rule has no safety content. A single-word «in» cannot
        // capture anything, because capture needs a multi-word name straddling a
        // hole, which is what GlueInName governs.
        var single = Assert.IsType<GlueAsName>(Assert.Single(Of("var in => Number;\n")));

        Assert.Equal("in", single.Name);
        Assert.Equal("for each (_) in (_)", single.Pattern);

        // and a multi-word name containing it is the capture case, which is the
        // one that matters
        var straddling = Assert.IsType<GlueInName>(Assert.Single(Of("var in flight order => Number;\n")));

        Assert.Equal("in flight order", straddling.Name);
        Assert.Equal("for each (_) in (_)", straddling.Pattern);
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
        // for this spelling collapses. Shown with «to» rather than «in», since
        // the lexer now takes «in» first.
        var findings = Of("""
                          function send (x => Number) to (y => Number) { return x; }
                          for each hello to alice in orders { return alice; }

                          """);

        var glue = Assert.IsType<GlueInName>(Assert.Single(findings, finding => finding is GlueInName));

        Assert.Equal("hello to alice", glue.Name);
        Assert.Equal("to", glue.Word);

        // the span is on the variable inside the loop, not on the pattern that
        // made the word glue
        Assert.Contains("for each ", glue.Primary.Source.Text[..glue.Primary.Offset]);
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
                                                                var bank => Number;
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
        Assert.Empty(Of("function for (x => Number) { return x; }\n"));

        // «for» is still an ordinary word, which is the point of spelling the
        // keyword «for each» — «compute total for (_)» is a pattern the language
        // wants and reserving «for» would have taken it away
        Assert.Empty(Of("function compute total for (order => Number) { return order; }\n"));
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
