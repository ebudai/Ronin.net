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

    [Fact(DisplayName = "a declaration is a name or a pattern, structurally")]
    public void ADeclarationIsANameOrAPatternStructurally()
    {
        var declared = Of("""
            var base price => Number;
            let tax => Number;
            var late bound => reactive Number;
            function compute total for (order => Number) { return order; }
            """);

        Assert.Empty(declared.Problems);

        // All declarations are names; the let and explicitly reactive var are
        // marked reactive for «old (_)». No shadow name is injected.
        Assert.Equal(["base price", "late bound", "tax"], declared.Symbols.Names.Order());
        Assert.Equal("NoParse", new Resolver(declared.Symbols).Resolve("old base price").Kind.ToString());
        Assert.Equal("Resolved", new Resolver(declared.Symbols).Resolve("old tax").Kind.ToString());
        Assert.Equal("Resolved", new Resolver(declared.Symbols).Resolve("old late bound").Kind.ToString());

        // and the function, whose parameter block became the hole
        var pattern = Assert.Single(declared.Symbols.Patterns);
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
            var wheel => Number;
            function area of (radius => Number) { return radius; }
            function area of (shape => Text) { return shape; }
            """);

        Assert.True(new Resolver(declared.Symbols).Resolve(Lexemes.Lex("area of wheel")).TryTree(out var tree));

        var call = Assert.IsType<Node.Call>(tree);

        // Both, and told apart by the thing that will decide between them.
        Assert.Equal([["Number"], ["Text"]],
                     declared.Overloads[call.Pattern].Select(signature => Assert.Single(signature.Types)));
    }

    [Fact(DisplayName = "and an unoverloaded call reaches exactly one")]
    public void AndAnUnoverloadedCallReachesExactlyOne()
    {
        // The common case, which has to stay a set of one rather than become a
        // special case: narrowing a singleton is the identity, so a type filter
        // written against the set above does nothing here and costs nothing.
        var declared = Of("""
            var wheel => Number;
            function area of (radius => Number) { return radius; }
            """);

        Assert.True(new Resolver(declared.Symbols).Resolve(Lexemes.Lex("area of wheel")).TryTree(out var tree));

        var only = Assert.Single(declared.Overloads[Assert.IsType<Node.Call>(tree).Pattern]);

        Assert.Equal([["radius"]], only.Names);
        Assert.Equal([["Number"]], only.Types);
    }

    [Fact(DisplayName = "overloads are one shape, not one ambiguity")]
    public void OverloadsAreOneShapeNotOneAmbiguity()
    {
        // Two declarations sharing a shape are two things a call could mean, not
        // two ways to read it. Inserting both made every call to an overloaded
        // shape come back ambiguous, which is R3 answering a question nobody
        // asked it.
        var declared = Of("""
            var wheel => Number;
            function area of (radius => Number) { return radius; }
            function area of (shape => Text) { return shape; }
            """);

        var pattern = Assert.Single(declared.Symbols.Patterns);
        Assert.Equal(2, declared.Overloads[pattern].Count);

        // WHAT THEY DIFFER IN, which the record could not say. Two declarations
        // of one shape were the same entry twice — a genuine duplicate and an
        // overload were indistinguishable, so the rule refusing both could not
        // name which it was refusing, and a narrowing pass would have had
        // nothing to narrow on.
        Assert.Equal([["Number"], ["Text"]],
                     declared.Overloads[pattern].Select(signature => Assert.Single(signature.Types)));

        // And an OMITTED type is null rather than blank, because omission is a
        // type and not a gap: «print (x)» is the generic declaration and «print
        // (x => Number)» is a different one, so a record that flattened the two
        // would make the generic look like a duplicate of whichever concrete
        // declaration was written beside it.
        var mixed = Of("function volume of (shape => Text) and (n) { return shape; }");

        Assert.Equal([["Text"], [null]],
                     Assert.Single(mixed.Overloads[Assert.Single(mixed.Symbols.Patterns)]).Types);

        Assert.Equal("area of «wheel»",
                     new Resolver(declared.Symbols).Resolve(Lexemes.Lex("area of wheel")).Reading);

        // and the thing that is actually missing says so
        var problem = Assert.Single(declared.Problems);

        Assert.Equal(FindingKind.Overloaded, problem.Kind);
        var overloaded = Assert.IsType<Overloaded>(problem);

        Assert.Equal("area of (_)", overloaded.Pattern);
        Assert.Equal(2, overloaded.Count);
    }

    [Fact(DisplayName = "a constant and an imperative variable are not reactive")]
    public void AConstantAndAnImperativeVariableAreNotReactive()
    {
        var declared = Of("constant pi => Number; var radius => Number;");

        Assert.Equal(["pi", "radius"], declared.Symbols.Names.Order());
        Assert.Contains("is a constant", declared.Symbols.Explain("old pi"));
        Assert.Contains("not reactive", declared.Symbols.Explain("old radius"));
    }

    [Fact(DisplayName = "the scope it builds resolves the statements beside it")]
    public void TheScopeItBuildsResolvesTheStatementsBesideIt()
    {
        // the point of the whole pass: a file's own declarations are what its
        // statements are read against
        var declared = Of("""
            let base price => reactive Number;
            var tax => Number;
            function compute total for (amount => Number) { return amount; }
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
        var declared = Of("function draw (shape => Text) at (x => Number, y => Number) { return shape; }");

        var pattern = Assert.Single(declared.Symbols.Patterns);
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
        Assert.Equal([["order"]], Assert.Single(defaulted.Overloads[Assert.Single(defaulted.Symbols.Patterns)]).Names);

        // «function fetch (the ball)» is the guide's own example
        var bare = Of("function fetch (the ball) { return the ball; }");

        Assert.Empty(bare.Problems);
        Assert.Equal([["the ball"]], Assert.Single(bare.Overloads[Assert.Single(bare.Symbols.Patterns)]).Names);
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
        var declared = Nested("var base price => Number;", "var discount => Number;");

        Assert.Empty(declared.Problems);
        Assert.Equal(["base price", "discount"], declared.Symbols.Names.Order());

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
            constant pi => Number;
            function area of (radius => Number) { return radius; }
            """,
            "var wheel => Number;");

        Assert.Empty(declared.Problems);

        var pattern = Assert.Single(declared.Symbols.Patterns);
        Assert.Equal([["radius"]], Assert.Single(declared.Overloads[pattern]).Names);

        // And an OMITTED type is null rather than blank, because omission is a
        // type and not a gap: «print (x)» is the generic declaration and «print
        // (x => Number)» is a different one, so a record that flattened the two
        // would make the generic look like a duplicate of whichever concrete
        // declaration was written beside it.
        var mixed = Of("function volume of (shape => Text) and (n) { return shape; }");

        Assert.Equal([["Text"], [null]],
                     Assert.Single(mixed.Overloads[Assert.Single(mixed.Symbols.Patterns)]).Types);

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
        var enclosing = Of("var outer thing => Number;");
        Nested("var outer thing => Number;", "var inner thing => Number;");

        Assert.DoesNotContain("inner thing", enclosing.Symbols.Names);
    }

    [Fact(DisplayName = "shadowing an enclosing name is rejected where it is written")]
    public void ShadowingAnEnclosingNameIsRejectedWhereItIsWritten()
    {
        var declared = Nested("var total => Number;", "var total => Number;");

        var problem = Assert.Single(declared.Problems);

        Assert.Equal(FindingKind.Shadowed, problem.Kind);
        Assert.Equal("total", Assert.IsType<Shadowed>(problem).Name);
        Assert.Equal("in an enclosing scope", Assert.IsType<Shadowed>(problem).Where);

        // and a repeat within one scope says so differently
        var twice = Assert.Single(Of("var total => Number; var total => Number;").Problems);
        Assert.Equal("in this scope", Assert.IsType<Shadowed>(twice).Where);
    }

    [Fact(DisplayName = "a name may not cover the built-in old pattern")]
    public void ANameMayNotCoverTheBuiltinOldPattern()
    {
        var declared = Of("var old total => Number;");

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

        Assert.Equal(["Dog", "pet"], declared.Symbols.Names.Order());
    }

    [Fact(DisplayName = "statements that declare nothing declare nothing")]
    public void StatementsThatDeclareNothingDeclareNothing()
    {
        // an expression mentions names, an assignment writes one, and neither
        // introduces one
        var declared = Of("var x => Number; x + x; x = 3;");

        Assert.Equal(["x"], declared.Symbols.Names.Order());
        Assert.Empty(declared.Symbols.Patterns);
    }
}
