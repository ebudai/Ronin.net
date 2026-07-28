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
        Lexer lexer = new(source);
        Parser parser = new(lexer.Lex());

        return Declarations.Of(parser.Parse().Scopes[0].Statements);
    }

    [Fact(DisplayName = "a declaration is a name or a pattern, structurally")]
    public void ADeclarationIsANameOrAPatternStructurally()
    {
        var declared = Of("""
            var base price => Number;
            let tax => Number;
            function compute total for (order => Number) { return order; }
            """);

        Assert.Empty(declared.Problems);

        // the two cells, each with its shadow injected
        Assert.Equal(["base price", "old base price", "old tax", "tax"], declared.Symbols.Names.Order());

        // and the function, whose parameter block became the hole
        var pattern = Assert.Single(declared.Symbols.Patterns);
        Assert.Equal("compute total for (_)", pattern.ToString());
        Assert.Equal([["order"]], Assert.Single(declared.Overloads[pattern]));
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

        Assert.Equal("area of «wheel»",
                     new Resolver(declared.Symbols).Resolve(Lexemes.Lex("area of wheel")).Reading);

        // and the thing that is actually missing says so
        Assert.Contains("type-directed selection is not implemented",
                        Assert.Single(declared.Problems));
    }

    [Fact(DisplayName = "a constant is named but gets no shadow")]
    public void AConstantIsNamedButGetsNoShadow()
    {
        var declared = Of("constant pi => Number; var radius => Number;");

        Assert.Equal(["old radius", "pi", "radius"], declared.Symbols.Names.Order());
        Assert.Contains("is a constant", declared.Symbols.Explain("old pi"));
    }

    [Fact(DisplayName = "the scope it builds resolves the statements beside it")]
    public void TheScopeItBuildsResolvesTheStatementsBesideIt()
    {
        // the point of the whole pass: a file's own declarations are what its
        // statements are read against
        var declared = Of("""
            var base price => Number;
            var tax => Number;
            function compute total for (amount => Number) { return amount; }
            """);

        Resolver resolver = new(declared.Symbols);

        Assert.Equal("compute total for («base price» + «tax»)",
                     resolver.Resolve(Lexemes.Lex("compute total for base price + tax")).Reading);

        // and «old» is in scope during resolution, unconditionally, because
        // whether anything reads it is not known until after
        Assert.Equal("(«base price» - «old base price»)",
                     resolver.Resolve(Lexemes.Lex("base price - old base price")).Reading);
    }

    [Fact(DisplayName = "several holes become several blocks, in order")]
    public void SeveralHolesBecomeSeveralBlocksInOrder()
    {
        var declared = Of("function draw (shape => Text) at (x => Number, y => Number) { return shape; }");

        var pattern = Assert.Single(declared.Symbols.Patterns);
        Assert.Equal("draw (_) at (_)", pattern.ToString());
        Assert.Equal([["shape"], ["x", "y"]], Assert.Single(declared.Overloads[pattern]));
    }

    [Fact(DisplayName = "a parameter it cannot name is reported, not guessed")]
    public void AParameterItCannotNameIsReportedNotGuessed()
    {
        // «(order = 3)» is a defaulted parameter, which parses as an assignment
        // rather than a declaration; producing a block with a null in it would be
        // worse than saying so
        var declared = Of("function compute total for (order = 3) { return order; }");

        Assert.Empty(declared.Symbols.Patterns);

        var problem = Assert.Single(declared.Problems);
        Assert.Contains("1 parameter(s) this pass cannot name", problem);
    }

    private static Declarations Nested(string outer, string inner)
    {
        Lexer lexer = new(outer);
        Parser parser = new(lexer.Lex());
        var enclosing = Declarations.Of(parser.Parse().Scopes[0].Statements);

        Lexer within = new(inner);
        Parser nested = new(within.Lex());
        return Declarations.Of(nested.Parse().Scopes[0].Statements, enclosing);
    }

    [Fact(DisplayName = "an inner scope sees the enclosing one, flattened")]
    public void AnInnerScopeSeesTheEnclosingOneFlattened()
    {
        // Inward yes: a lookup is one probe against a merged table rather than a
        // walk up the chain, which is what banning shadowing buys.
        var declared = Nested("var base price => Number;", "var discount => Number;");

        Assert.Empty(declared.Problems);
        Assert.Equal(["base price", "discount", "old base price", "old discount"],
                     declared.Symbols.Names.Order());

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
        Assert.Equal([["radius"]], Assert.Single(declared.Overloads[pattern]));

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
        Lexer lexer = new("var outer thing => Number;");
        Parser parser = new(lexer.Lex());
        var enclosing = Declarations.Of(parser.Parse().Scopes[0].Statements);

        Lexer within = new("var inner thing => Number;");
        Parser nested = new(within.Lex());
        Declarations.Of(nested.Parse().Scopes[0].Statements, enclosing);

        Assert.DoesNotContain("inner thing", enclosing.Symbols.Names);
    }

    [Fact(DisplayName = "shadowing an enclosing name is rejected where it is written")]
    public void ShadowingAnEnclosingNameIsRejectedWhereItIsWritten()
    {
        var declared = Nested("var total => Number;", "var total => Number;");

        var problem = Assert.Single(declared.Problems);
        Assert.Contains("«total» is already declared in an enclosing scope", problem);

        // and a repeat within one scope says so differently
        var twice = Of("var total => Number; var total => Number;");
        Assert.Contains("in this scope", Assert.Single(twice.Problems));
    }

    [Fact(DisplayName = "a name may not be spelled like an injected one")]
    public void ANameMayNotBeSpelledLikeAnInjectedOne()
    {
        var declared = Of("var old total => Number;");

        Assert.Contains("reserved word «old»", Assert.Single(declared.Problems));
    }

    [Fact(DisplayName = "an inner pattern that breaks an outer name is the one rejected")]
    public void AnInnerPatternThatBreaksAnOuterNameIsTheOneRejected()
    {
        // R5 applies to the merged table, so an inner declaration can invalidate
        // an outer one — and the later declaration is the site of the mistake.
        //
        // Glue is the literal words AFTER the first hole, so «send (_) to (_)»
        // makes «to» glue while «compute total for (_)» makes nothing glue at
        // all: every word of it is anchor.
        var declared = Nested("var hello to alice => Number;",
                              "function send (x => Number) to (y => Number) { return x; }");

        var complaint = Assert.Single(declared.Symbols.Validate(),
                                      problem => problem.Contains("«hello to alice»"));

        Assert.Contains("«to»", complaint);
        Assert.Contains("«send (_) to (_)»", complaint);
        Assert.Contains("later declaration", complaint);
    }

    [Fact(DisplayName = "a type is a name that holds no value")]
    public void ATypeIsANameThatHoldsNoValue()
    {
        // named so it can be referred to, but no shadow: only a cell has a
        // previous value
        var declared = Of("type Dog { } var pet => Dog;");

        Assert.Equal(["Dog", "old pet", "pet"], declared.Symbols.Names.Order());
    }

    [Fact(DisplayName = "statements that declare nothing declare nothing")]
    public void StatementsThatDeclareNothingDeclareNothing()
    {
        // an expression mentions names, an assignment writes one, and neither
        // introduces one
        var declared = Of("var x => Number; x + x; x = 3;");

        Assert.Equal(["old x", "x"], declared.Symbols.Names.Order());
        Assert.Empty(declared.Symbols.Patterns);
    }
}
