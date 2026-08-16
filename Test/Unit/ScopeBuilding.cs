// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Declarations = Ronin.Grammar.Declarations;

namespace Unit;

/// <summary>
///     A parsed scope becoming the resolver's scope.
/// </summary>
[Trait(nameof(Declarations), null)]
public class ScopeBuilding
{
    private static Declarations Of(string source)
    {
        SourceText text = new(source);
        Lexer lexer = new(source);
        Parser parser = new(lexer.Lex());

        return Declarations.Of(parser.Parse().Scopes[0].Statements, text);
    }

    /// <summary>
    ///     The over-declaration findings a scope's shapes classify to. «Compilation»
    ///     classifies against a shape's whole container, folding several bodies into
    ///     one table; here each shape's types are all in this one table, so the two
    ///     agree, and the classifier's own rules are what these exercise.
    /// </summary>
    private static System.Collections.Generic.IReadOnlyList<Finding> Classified(string source)
    {
        var declared = Of(source);

        System.Collections.Generic.List<Finding> found = [];

        foreach (var pattern in declared.Overloads.Keys) found.AddRange(declared.Classify(pattern));

        return found;
    }

    [Fact(DisplayName = "declaring a supplied type name says the language supplies it, not that you declared it twice")]
    public void DeclaringASuppliedTypeNameSaysTheLanguageSuppliesItNotThatYouDeclaredItTwice()
    {
        var refused = Assert.Single(Of("var number => number;\n").Problems);

        // BOTH would be refusals and both would be correct; they are different
        // findings because the remedies differ. «Shadowed» says you declared this
        // name twice — rename either, both are yours. «Supplied» says the
        // language owns the spelling, so only one of the two is the author's to
        // move, and a message telling someone to rename one of two things when
        // one of them is not theirs wastes a minute of reading.
        Assert.IsType<Supplied>(refused);
    }

    [Fact(DisplayName = "a supplied type name is not a truth literal")]
    public void ASuppliedTypeNameIsNotATruthLiteral()
    {
        // «Truths» takes every NULLARY supply, so the four supplied type names
        // would have become truth literals the moment they were added — an
        // expression could mention «number» and get a value. That is the kind
        // filter's absence showing up as a wrong answer rather than a missing
        // one, which is the shape of the annotation-prune bug, so it is named.
        Assert.Equal(["false", "true"], SymbolTable.Truths.Order());

        // They ARE supplied, so nobody may declare them — the language owns the
        // spelling and only one of the two names would be the author's to move.
        Assert.Contains("number", SymbolTable.Whole);
        Assert.Contains("error", SymbolTable.Whole);

        // And no expression may mention one.
        Assert.Equal(ResolutionKind.NoParse, new Resolver(new SymbolTable()).Resolve(Lexemes.Lex("number")).Kind);
    }

    [Fact(DisplayName = "a type is a name in the same table, told apart by its kind")]
    public void ATypeIsANameInTheSameTableToldApartByItsKind()
    {
        var declared = Of("type money;\nvar cash on hand => number;\n");

        // ONE table. A second would need the position to choose between them, and
        // «type of x» is the case that cannot answer — it puts a type exactly
        // where a value goes. So both are here, and the kind is what tells them
        // apart rather than where they live.
        Assert.Equal(["cash on hand", "money"], declared.Symbols.Names.Keys.Order());

        Assert.Equal(SymbolKind.Type, declared.Symbols.Names["money"]);
        Assert.Equal(SymbolKind.Value, declared.Symbols.Names["cash on hand"]);

        // And the kind NARROWS the candidates rather than merely labelling them:
        // an expression may mention the value and not the type, which is one pass
        // with two predicates rather than two tables with one each.
        var resolver = new Resolver(declared.Symbols);

        Assert.Equal(ResolutionKind.Resolved, resolver.Resolve(Lexemes.Lex("cash on hand")).Kind);
        Assert.Equal(ResolutionKind.NoParse, resolver.Resolve(Lexemes.Lex("money")).Kind);
    }

    [Fact(DisplayName = "a declaration is a name or a pattern, structurally")]
    public void ADeclarationIsANameOrAPatternStructurally()
    {
        var declared = Of("""
            var base price => number;
            let tax => number;
            var late bound => reactive number;
            function compute total for (order => number) { return order; }
            """);

        Assert.Empty(declared.Problems);

        // All declarations are names; the let and explicitly reactive var are
        // marked reactive for «old (_)». No shadow name is injected.
        Assert.Equal(["base price", "late bound", "tax"], declared.Symbols.Names.Keys.Order());
        Assert.Equal("NoParse", new Resolver(declared.Symbols).Resolve("old base price").Kind.ToString());
        Assert.Equal("Resolved", new Resolver(declared.Symbols).Resolve("old tax").Kind.ToString());
        Assert.Equal("Resolved", new Resolver(declared.Symbols).Resolve("old late bound").Kind.ToString());

        // and the function, whose parameter block became the hole
        var pattern = Assert.Single(declared.Symbols.Patterns).Pattern;
        Assert.Equal("compute total for (_)", pattern.ToString());
        Assert.Equal([["order"]], Assert.Single(declared.Overloads[pattern]).Names);
    }

    [Fact(DisplayName = "a resolved call reaches every declaration it could mean")]
    public void AResolvedCallReachesEveryDeclarationItCouldMean()
    {
        // The seam a type filter narrows on, pinned before there is one.
        //
        // The design expected a CANDIDATE SET to be a missing field on the call
        // node. It is not missing: a call carries its shape, the shape is what
        // «Overloads» is keyed by, and the set is a lookup away — which is also
        // why the runtime can already say "ambiguous after type filtering" about
        // a case nothing filters yet.
        //
        // What a field would hold is the NARROWED set, which is per call site
        // rather than per shape. Nothing narrows, so nothing would fill it, and
        // a slot with no producer is the sort of thing this suite deletes rather
        // than tests. It can arrive with the pass that needs it.
        var declared = Of("""
            var wheel => number;
            function area of (radius => number) { return radius; }
            function area of (shape => text) { return shape; }
            """);

        Assert.True(new Resolver(declared.Symbols).Resolve(Lexemes.Lex("area of wheel")).TryTree(out var tree));

        var call = Assert.IsType<Node.Call>(tree);

        // Both, and told apart by the thing that will decide between them.
        Assert.Equal([["number"], ["text"]],
                     declared.Overloads[call.Pattern].Select(signature => Assert.Single(signature.Types)));
    }

    [Fact(DisplayName = "and an unoverloaded call reaches exactly one")]
    public void AndAnUnoverloadedCallReachesExactlyOne()
    {
        // The common case, which has to stay a set of one rather than become a
        // special case: narrowing a singleton is the identity, so a type filter
        // written against the set above does nothing here and costs nothing.
        var declared = Of("""
            var wheel => number;
            function area of (radius => number) { return radius; }
            """);

        Assert.True(new Resolver(declared.Symbols).Resolve(Lexemes.Lex("area of wheel")).TryTree(out var tree));

        var only = Assert.Single(declared.Overloads[Assert.IsType<Node.Call>(tree).Pattern]);

        Assert.Equal([["radius"]], only.Names);
        Assert.Equal([["number"]], only.Types);
    }

    [Theory(DisplayName = "a shape declared twice is two different mistakes")]
    [InlineData("(radius => number)", "(shape => text)", "Overloaded")]
    [InlineData("(box => number)", "(frame => number)", "DuplicateSignature")]
    [InlineData("(box => number)", "(frame)", "Overloaded")]
    public void AShapeDeclaredTwiceIsTwoDifferentMistakes(string one, string other, string refused)
    {
        // TWO RULES wearing one name, and only one of them expires. Declarations
        // whose parameter types DIFFER are waiting for type-directed selection.
        // Declarations whose types are the SAME are waiting for nothing — no
        // type information could ever tell them apart — so that one is a
        // duplicate and always will be.
        //
        // Sharing a diagnostic meant landing the type checker would have meant
        // picking the two apart under time pressure, which is what a ledger
        // entry recording only "expires" schedules.
        //
        // The third row is the one that makes «same types» mean something: an
        // OMITTED type is generic, so it differs from a written one rather than
        // matching everything.
        Assert.Equal(refused, Assert.Single(Classified($"function area of {one} {{ return 1; }}\n"
                                                     + $"function area of {other} {{ return 1; }}\n")).Kind.ToString());
    }

    [Fact(DisplayName = "and what a parameter is called is not part of the difference")]
    public void AndWhatAParameterIsCalledIsNotPartOfTheDifference()
        // «area of (radius => number)» and «area of (r => number)» are the same
        // declaration written twice, and a caller cannot tell which of them they
        // reached. What a parameter is called is the callee's business.
        => Assert.Equal(FindingKind.DuplicateSignature,
                        Assert.Single(Classified("function area of (radius => number) { return 1; }\n"
                                               + "function area of (r => number) { return 1; }\n")).Kind);

    [Fact(DisplayName = "and two spellings of one type are a duplicate, not an overload")]
    public void AndTwoSpellingsOfOneTypeAreADuplicateNotAnOverload()
        // REAUDIT54 finding 3: «number» and «(number)» resolve to the same sort, so
        // «use (x => number)» and «use (x => (number))» are one signature written
        // twice — a duplicate that must survive, not an overload waiting to expire
        // into a use-site selection. Keying the classifier by spelling filed them
        // apart, so the expiry would one day have made a genuine duplicate legal.
        => Assert.Equal(FindingKind.DuplicateSignature,
                        Assert.Single(Classified("function use (x => number) { return x; }\n"
                                               + "function use (x => (number)) { return x; }\n")).Kind);

    [Fact(DisplayName = "and the same types split into different blocks are an overload, not a duplicate")]
    public void AndTheSameTypesSplitIntoDifferentBlocksAreAnOverloadNotADuplicate()
    {
        // «(a, b) with (c)» and «(a) with (b, c)» carry the same three types, but
        // spread differently across the two holes — a caller brackets them
        // differently and a type checker can tell them apart, so they are an
        // overload waiting for one, not a duplicate. Concatenating the blocks
        // flattened both to «Number Text Number» and refused them permanently.
        Assert.Equal(FindingKind.Overloaded,
                     Assert.Single(Classified("function arrange (a => number, b => text) with (c => number) { return a; }\n"
                                            + "function arrange (a => number) with (b => text, c => number) { return a; }\n")).Kind);
    }

    [Fact(DisplayName = "and a duplicate among overloads is reported apart from the overload it hides in")]
    public void AndADuplicateAmongOverloadsIsReportedApartFromTheOverloadItHidesIn()
    {
        // «A, A, B»: the two A's are a duplicate nothing can ever choose between,
        // and the A's against the B are an overload waiting for a type checker.
        // One finding spanning the first declaration and the last stood for both
        // — and the first and last are «A» and «B», which are not the colliding
        // pair. Both findings are reported now, each against the declarations it
        // is actually about.
        const string Source = "function area of (a => number) { return a; }\n"   // A
                            + "function area of (b => number) { return b; }\n"   // A again — the duplicate
                            + "function area of (c => text) { return c; }\n";    // B — a distinct overload

        var problems = Classified(Source);

        Assert.Equal(2, problems.Count);

        int Where(string mark) => Source.IndexOf(mark, StringComparison.Ordinal);

        // the duplicate names the two «Number» declarations, which are what
        // collide — not the first and the last
        var duplicate = Assert.IsType<DuplicateSignature>(problems.Single(problem => problem.Kind is FindingKind.DuplicateSignature));

        Assert.Equal(Where("area of (a"), duplicate.Primary.Offset);
        Assert.Equal(Where("area of (b"), Assert.Single(duplicate.Related).Span.Offset);

        // and the overload names the two distinct groups — the «Number» set and
        // the «Text» one — because removing a duplicate does not make those
        // choosable
        var overloaded = Assert.IsType<Overloaded>(problems.Single(problem => problem.Kind is FindingKind.Overloaded));

        Assert.Equal(2, overloaded.Count);
        Assert.Equal(Where("area of (a"), overloaded.Primary.Offset);
        Assert.Equal(Where("area of (c"), Assert.Single(overloaded.Related).Span.Offset);
    }

    [Fact(DisplayName = "overloads are one shape, not one ambiguity")]
    public void OverloadsAreOneShapeNotOneAmbiguity()
    {
        // Two declarations sharing a shape are two things a call could mean, not
        // two ways to read it. Inserting both made every call to an overloaded
        // shape come back ambiguous, which is R3 answering a question nobody
        // asked it.
        var source = """
            var wheel => number;
            function area of (radius => number) { return radius; }
            function area of (shape => text) { return shape; }
            """;

        var declared = Of(source);

        var pattern = Assert.Single(declared.Symbols.Patterns).Pattern;
        Assert.Equal(2, declared.Overloads[pattern].Count);

        // WHAT THEY DIFFER IN, which the record could not say. Two declarations
        // of one shape were the same entry twice — a genuine duplicate and an
        // overload were indistinguishable, so the rule refusing both could not
        // name which it was refusing, and a narrowing pass would have had
        // nothing to narrow on.
        Assert.Equal([["number"], ["text"]],
                     declared.Overloads[pattern].Select(signature => Assert.Single(signature.Types)));

        // And an OMITTED type is null rather than blank, because omission is a
        // type and not a gap: «print (x)» is the generic declaration and «print
        // (x => number)» is a different one, so a record that flattened the two
        // would make the generic look like a duplicate of whichever concrete
        // declaration was written beside it.
        var mixed = Of("function volume of (shape => text) and (n) { return shape; }");

        Assert.Equal([["text"], [null]],
                     Assert.Single(mixed.Overloads[Assert.Single(mixed.Symbols.Patterns).Pattern]).Types);

        Assert.Equal("area of «wheel»",
                     new Resolver(declared.Symbols).Resolve(Lexemes.Lex("area of wheel")).Reading);

        // and the thing that is actually missing says so
        var problem = Assert.Single(Classified(source));

        Assert.Equal(FindingKind.Overloaded, problem.Kind);
        var overloaded = Assert.IsType<Overloaded>(problem);

        Assert.Equal("area of (_)", overloaded.Pattern);
        Assert.Equal(2, overloaded.Count);
    }

    [Fact(DisplayName = "a constant and an imperative variable are not reactive")]
    public void AConstantAndAnImperativeVariableAreNotReactive()
    {
        var declared = Of("constant pi => number; var radius => number;");

        Assert.Equal(["pi", "radius"], declared.Symbols.Names.Keys.Order());
        Assert.Contains("is a constant", declared.Symbols.Explain("old pi"));
        Assert.Contains("not reactive", declared.Symbols.Explain("old radius"));
    }

    [Fact(DisplayName = "the scope it builds resolves the statements beside it")]
    public void TheScopeItBuildsResolvesTheStatementsBesideIt()
    {
        // the point of the whole pass: a file's own declarations are what its
        // statements are read against
        var declared = Of("""
            let base price => reactive number;
            var tax => number;
            function compute total for (amount => number) { return amount; }
            """);

        Resolver resolver = new(declared.Symbols);

        Assert.Equal("compute total for («base price» + «tax»)",
                     resolver.Resolve(Lexemes.Lex("compute total for base price + tax")).Reading);

        // The pattern is always part of the language, and this reference is
        // admitted because the declaration records that base price is reactive.
        Assert.Equal("(«base price» - old «base price»)",
                     resolver.Resolve(Lexemes.Lex("base price - old base price")).Reading);
    }

    [Fact(DisplayName = "several holes become several blocks, in order")]
    public void SeveralHolesBecomeSeveralBlocksInOrder()
    {
        var declared = Of("function draw (shape => text) at (x => number, y => number) { return shape; }");

        var pattern = Assert.Single(declared.Symbols.Patterns).Pattern;
        Assert.Equal("draw (_) at (_)", pattern.ToString());
        Assert.Equal([["shape"], ["x", "y"]], Assert.Single(declared.Overloads[pattern]).Names);
    }

    [Fact(DisplayName = "a parameter may be bare or defaulted")]
    public void AParameterMayBeBareOrDefaulted()
    {
        // Both were rejected while parameters shared the statement guard, which
        // is what keeps «order = 3» an assignment in a body. In parameter
        // position there is nothing to be confused with.
        var defaulted = Of("function compute total for (order = 3) { return order; }");

        Assert.Empty(defaulted.Problems);
        Assert.Equal([["order"]], Assert.Single(defaulted.Overloads[Assert.Single(defaulted.Symbols.Patterns).Pattern]).Names);

        // «function fetch (the ball)» is the guide's own example
        var bare = Of("function fetch (the ball) { return the ball; }");

        Assert.Empty(bare.Problems);
        Assert.Equal([["the ball"]], Assert.Single(bare.Overloads[Assert.Single(bare.Symbols.Patterns).Pattern]).Names);
    }

    private static Declarations Nested(string outer, string inner)
    {
        var enclosing = Of(outer);

        SourceText text = new(inner);
        Lexer within = new(inner);
        Parser nested = new(within.Lex());

        return Declarations.Of(nested.Parse().Scopes[0].Statements, text, enclosing);
    }

    [Fact(DisplayName = "an inner scope sees the enclosing one, flattened")]
    public void AnInnerScopeSeesTheEnclosingOneFlattened()
    {
        // Inward yes: a lookup is one probe against a merged table rather than a
        // walk up the chain, which is what banning shadowing buys.
        var declared = Nested("var base price => number;", "var discount => number;");

        Assert.Empty(declared.Problems);
        Assert.Equal(["base price", "discount"], declared.Symbols.Names.Keys.Order());

        Assert.Equal("(«base price» - «discount»)",
                     new Resolver(declared.Symbols).Resolve(Lexemes.Lex("base price - discount")).Reading);
    }

    [Fact(DisplayName = "an inner scope inherits patterns and constants too")]
    public void AnInnerScopeInheritsPatternsAndConstantsToo()
    {
        // everything the enclosing scope declared, not only its cells: the inner
        // scope can call the outer function and read the outer constant, and the
        // constant is still known to be one
        var declared = Nested("""
            constant pi => number;
            function area of (radius => number) { return radius; }
            """,
            "var wheel => number;");

        Assert.Empty(declared.Problems);

        var pattern = Assert.Single(declared.Symbols.Patterns).Pattern;
        Assert.Equal([["radius"]], Assert.Single(declared.Overloads[pattern]).Names);

        // And an OMITTED type is null rather than blank, because omission is a
        // type and not a gap: «print (x)» is the generic declaration and «print
        // (x => number)» is a different one, so a record that flattened the two
        // would make the generic look like a duplicate of whichever concrete
        // declaration was written beside it.
        var mixed = Of("function volume of (shape => text) and (n) { return shape; }");

        Assert.Equal([["text"], [null]],
                     Assert.Single(mixed.Overloads[Assert.Single(mixed.Symbols.Patterns).Pattern]).Types);

        Assert.Equal("area of «wheel»",
                     new Resolver(declared.Symbols).Resolve(Lexemes.Lex("area of wheel")).Reading);

        // inherited constants stay constants, so «old pi» still explains itself
        Assert.Contains("is a constant", declared.Symbols.Explain("old pi"));
    }

    [Fact(DisplayName = "nothing an inner scope declares escapes it")]
    public void NothingAnInnerScopeDeclaresEscapesIt()
    {
        // outward no, because a pattern declaration is a grammar production and
        // an escaping one would change the grammar of its siblings' bodies
        var enclosing = Of("var outer thing => number;");
        Nested("var outer thing => number;", "var inner thing => number;");

        Assert.DoesNotContain("inner thing", enclosing.Symbols.Names);
    }

    [Fact(DisplayName = "shadowing an enclosing name is rejected where it is written")]
    public void ShadowingAnEnclosingNameIsRejectedWhereItIsWritten()
    {
        var declared = Nested("var total => number;", "var total => number;");

        var problem = Assert.Single(declared.Problems);

        Assert.Equal(FindingKind.Shadowed, problem.Kind);
        Assert.Equal("total", Assert.IsType<Shadowed>(problem).Name);
        Assert.Equal("in an enclosing scope", Assert.IsType<Shadowed>(problem).Where);

        // and a repeat within one scope says so differently
        var twice = Assert.Single(Of("var total => number; var total => number;").Problems);
        Assert.Equal("in this scope", Assert.IsType<Shadowed>(twice).Where);
    }

    [Fact(DisplayName = "a name may not cover the built-in old pattern")]
    public void ANameMayNotCoverTheBuiltinOldPattern()
    {
        var declared = Of("var old total => number;");

        var problem = Assert.Single(declared.Problems);

        Assert.Equal(FindingKind.NameShadowsPattern, problem.Kind);
        var shadows = Assert.IsType<NameShadowsPattern>(problem);
        Assert.True(shadows.Builtin);
        Assert.Equal("old total", shadows.Name);
    }

    [Fact(DisplayName = "a type is a name that holds no value")]
    public void ATypeIsANameThatHoldsNoValue()
    {
        // Named so it can be referred to. The imperative pet is not eligible
        // for «old (_)».
        var declared = Of("type Dog { } var pet => Dog;");

        Assert.Equal(["Dog", "pet"], declared.Symbols.Names.Keys.Order());
    }

    [Fact(DisplayName = "statements that declare nothing declare nothing")]
    public void StatementsThatDeclareNothingDeclareNothing()
    {
        // an expression mentions names, an assignment writes one, and neither
        // introduces one
        var declared = Of("var x => number; x + x; x = 3;");

        Assert.Equal(["x"], declared.Symbols.Names.Keys.Order());
        Assert.Empty(declared.Symbols.Patterns);
    }
}
