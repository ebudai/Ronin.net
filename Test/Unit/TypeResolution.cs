// Copyright © 2026 Eric Budai

using Ronin.Compiler;

namespace Unit;

/// <summary>
///     The resolver reading a type annotation rather than a value expression —
///     the same DP, the kind read the other way.
/// </summary>
///
/// <remarks>
///     Each suppression here is guarded by its own mirror: the value resolver
///     resolves what the type resolver refuses, so removing a gate makes a test
///     fail rather than pass quietly. A type position admits kind=type only, and
///     "only" is what these assert — a literal, an operator, a previous value and
///     a list are each a value, and none of them is a type.
/// </remarks>
[Trait(nameof(Resolver), null)]
public class TypeResolution
{
    private static Resolver Types(SymbolTable symbols) => new(symbols, kind: SymbolKind.Type);

    private static ResolutionKind AsType(SymbolTable symbols, string source)
        => Types(symbols).Resolve(source).Kind;

    private static ResolutionKind AsValue(SymbolTable symbols, string source)
        => new Resolver(symbols).Resolve(source).Kind;

    [Fact(DisplayName = "the supplied type names resolve as types and mention no value")]
    public void TheSuppliedTypeNamesResolveAsTypesAndMentionNoValue()
    {
        SymbolTable symbols = new();

        foreach (var type in SymbolTable.SuppliedTypes)
            Assert.Equal(ResolutionKind.Resolved, AsType(symbols, type));

        // The four supplied types, and no more — «true» and «false» are values of
        // «truth», not types, so a type annotation cannot mention them.
        Assert.Equal(["error", "number", "text", "truth"], SymbolTable.SuppliedTypes.Order());

        Assert.Equal(ResolutionKind.NoParse, AsType(symbols, "true"));
        Assert.Equal(ResolutionKind.NoParse, AsType(symbols, "false"));
    }

    [Fact(DisplayName = "the supplied type constructors resolve, and their bare anchors do not")]
    public void TheSuppliedTypeConstructorsResolveAndTheirBareAnchorsDoNot()
    {
        SymbolTable symbols = new();

        Assert.Equal(ResolutionKind.Resolved, AsType(symbols, "list of number"));
        Assert.Equal(ResolutionKind.Resolved, AsType(symbols, "optional text"));
        Assert.Equal(ResolutionKind.Resolved, AsType(symbols, "lookup text => number"));

        // Nesting composes, because a hole is filled by another type.
        Assert.Equal(ResolutionKind.Resolved, AsType(symbols, "optional list of number"));
        Assert.Equal(ResolutionKind.Resolved, AsType(symbols, "list of optional text"));

        // A bare anchor is not a type: the type is «list of (_)», and «list»
        // alone is a run of words nothing supplies. This is why a fixture's
        // «=> list» becomes «=> list of number».
        Assert.Equal(ResolutionKind.NoParse, AsType(symbols, "list"));
        Assert.Equal(ResolutionKind.NoParse, AsType(symbols, "optional"));
        Assert.Equal(ResolutionKind.NoParse, AsType(symbols, "lookup"));
    }

    [Fact(DisplayName = "a bracketed hole resolves, so the repair form of an ambiguity is a type")]
    public void ABracketedHoleResolvesSoTheRepairFormOfAnAmbiguityIsAType()
    {
        SymbolTable symbols = new();

        // Grouping is admitted in type position — a bracketed hole is how a
        // reader disambiguates, so it must itself resolve.
        Assert.Equal(ResolutionKind.Resolved, AsType(symbols, "list of (number)"));
        Assert.Equal(ResolutionKind.Resolved, AsType(symbols, "lookup (text) => (number)"));
    }

    [Fact(DisplayName = "a keyed round group is admitted, for the checker to refuse by multiplicity")]
    public void AKeyedRoundGroupIsAdmittedForTheCheckerToRefuseByMultiplicity()
    {
        // «optional (a = b)» — a round group with a key — is grouping, which type
        // position admits and the checker refuses later by multiplicity, per
        // TYPEHALFDECISIONS §3. The «=» is kept as a key rather than left inside a
        // span no expression consumes, which was a no-reading that read as
        // «optional (a = b) is not a type». A round group is never evaluated in a
        // type, so carrying the key as a lookup-shaped node costs nothing.
        var symbols = new SymbolTable().WithNames(SymbolKind.Type, "a", "b");

        Assert.Equal(ResolutionKind.Resolved, AsType(symbols, "optional (a = b)"));

        // and a value round group keyed the same way is still refused, because a
        // value «=» in round brackets means nothing — a lookup is «[a = b]».
        Assert.Equal(ResolutionKind.NoParse, AsValue(new SymbolTable().WithNames("a", "b"), "(a = b)"));
    }

    [Fact(DisplayName = "a declared type is mentionable as a type and not as a value; a value is the reverse")]
    public void ADeclaredTypeIsMentionableAsATypeAndNotAsAValueAValueIsTheReverse()
    {
        // «type money;» names a type; «var cash;» names a value. One table, and
        // the kind is what the annotation reads and the expression does not.
        var symbols = new SymbolTable().WithNames(SymbolKind.Type, "money").WithNames("cash");

        Assert.Equal(ResolutionKind.Resolved, AsType(symbols, "money"));
        Assert.Equal(ResolutionKind.NoParse, AsType(symbols, "cash"));

        Assert.Equal(ResolutionKind.Resolved, AsValue(symbols, "cash"));
        Assert.Equal(ResolutionKind.NoParse, AsValue(symbols, "money"));
    }

    [Fact(DisplayName = "a number literal is a value, so it is no part of a type")]
    public void ANumberLiteralIsAValueSoItIsNoPartOfAType()
    {
        SymbolTable symbols = new();

        // «list of 3» reaches the resolver where a bare «3» is a parser error, so
        // the literal offering is what would make it a type nothing checks. In
        // value position the same span past a pattern resolves, which is the
        // guard: the literal is offered there and refused here.
        Assert.Equal(ResolutionKind.NoParse, AsType(symbols, "list of 3"));
        Assert.Equal(ResolutionKind.Resolved, AsValue(new SymbolTable().WithPatterns("list of _"), "list of 3"));
    }

    [Fact(DisplayName = "an operator combines values, so it combines no types")]
    public void AnOperatorCombinesValuesSoItCombinesNoTypes()
    {
        // «number + text» is two type names either side of «+». In value position
        // «+» is an operator and joins two operands; in type position the
        // operator table is empty, so there is nothing to join them and the span
        // has no reading. The two mentionable names are the guard that it is «+»
        // and not the names that is missing.
        var symbols = new SymbolTable().WithNames("a", "b");

        Assert.Equal(ResolutionKind.NoParse, AsType(new SymbolTable(), "number + text"));
        Assert.Equal(ResolutionKind.Resolved, AsValue(symbols, "a + b"));
    }

    [Fact(DisplayName = "a previous value is a value, so it is no type")]
    public void APreviousValueIsAValueSoItIsNoType()
    {
        // «old (_)» reads a reactive name's previous generation — a value. The
        // reactive set lives in the one shared table, so nothing but the kind
        // keeps «old count» out of an annotation.
        var symbols = new SymbolTable().WithReactives("count");

        Assert.Equal(ResolutionKind.Resolved, AsValue(symbols, "old count"));
        Assert.Equal(ResolutionKind.NoParse, AsType(symbols, "old count"));
    }

    [Fact(DisplayName = "a list literal is a value, so it fills no type hole")]
    public void AListLiteralIsAValueSoItFillsNoTypeHole()
    {
        SymbolTable symbols = new();

        // «[ 3 ]» is a list value. Its brackets are the same LexemeKind as a
        // grouping «(…)», so only the kind tells the resolver that a «[…]» is
        // inadmissible where a type is wanted while a «(…)» is not.
        Assert.Equal(ResolutionKind.NoParse, AsType(symbols, "list of [ 3 ]"));
        Assert.Equal(ResolutionKind.Resolved, AsType(symbols, "list of (number)"));
    }

    [Fact(DisplayName = "a single arrow is a function type, and reads one way")]
    public void ASingleArrowIsAFunctionTypeAndReadsOneWay()
    {
        SymbolTable symbols = new();

        // «text => number» is the function type — a delegate's type, «() =>
        // number» and «(a, b) => c» in DELEGATES §1. One arrow, one reading.
        Assert.Equal(ResolutionKind.Resolved, AsType(symbols, "text => number"));

        // and the common two-arrow shape LOOKUP-ARROW §1 measured unique: the
        // kind filter admits only the reading where each arrow's operands are
        // types, so «lookup text => number» does not tie with a function type.
        Assert.Equal(ResolutionKind.Resolved, AsType(symbols, "lookup text => number"));
    }

    [Fact(DisplayName = "a function type takes any number of parameters, including none")]
    public void AFunctionTypeTakesAnyNumberOfParametersIncludingNone()
    {
        SymbolTable symbols = new();

        // «() => number» is the nullary function type — a zero-parameter callback,
        // «() => Number» in DELEGATES §1. Its «()» is an empty parameter list,
        // which is a type's to want and not a value's: an empty grouping in a
        // value is brackets round nothing.
        Assert.Equal(ResolutionKind.Resolved, AsType(symbols, "() => number"));
        Assert.Equal(ResolutionKind.Resolved, AsType(symbols, "(text, number) => truth"));

        // and the empty group is refused in value position, where it means nothing
        Assert.Equal(ResolutionKind.NoParse, AsValue(symbols, "()"));
    }

    [Fact(DisplayName = "a chain of arrows is an ambiguity, because the arrow does not associate")]
    public void AChainOfArrowsIsAnAmbiguityBecauseTheArrowDoesNotAssociate()
    {
        SymbolTable symbols = new();

        // «a => b => c» has no grouping to prefer where nothing is curried, so it
        // is refused rather than picked — two bracketings, both offered.
        var chain = Types(symbols).Resolve("text => number => truth");
        Assert.Equal(ResolutionKind.Ambiguous, chain.Kind);
        Assert.Equal(2, chain.Total);

        // LOOKUP-ARROW §2: the lookup's arrow-segment and the function arrow
        // compete for the second «=>», so there are THREE bracketings — a table
        // of callbacks, a table keyed by functions, and a function taking a table
        // — and all three are offered because the arrow binds at the pattern level.
        var lookups = Types(symbols).Resolve("lookup text => number => truth");
        Assert.Equal(ResolutionKind.Ambiguous, lookups.Kind);
        Assert.Equal(3, lookups.Total);
    }
}
