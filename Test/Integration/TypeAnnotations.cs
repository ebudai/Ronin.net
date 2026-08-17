// Copyright © 2026 Eric Budai

using Ronin.Compiler;

namespace Integration;

/// <summary>
///     Type annotations resolved through the compiler, by the path a build takes:
///     source in, findings out.
/// </summary>
///
/// <remarks>
///     The join <see cref="Compilation"/> deferred with a comment — «types resolve
///     against a table that does not exist yet» — now made. An annotation is a
///     reference read against the type kind, and a run of words that names no type
///     is a finding at the annotation rather than a no-reading nobody sees.
/// </remarks>
[Trait(nameof(Compilation), null)]
public class TypeAnnotations
{
    private static IReadOnlyList<Finding> Of(string source)
        => Compilation.Of(new SourceText(source, "Player.ron")).Findings;

    [Fact(DisplayName = "the supplied types annotate cleanly")]
    public void TheSuppliedTypesAnnotateCleanly()
    {
        Assert.Empty(Of("""
                        var a => number;
                        var b => text;
                        var c => truth;
                        var d => error;
                        var e => list of number;
                        var f => optional text;
                        var g => optional list of number;
                        var h => lookup text => number;
                        var i => text => number;
                        var j => lookup text => list of number;
                        var k => (text, number) => truth;
                        var l => () => number;

                        """));
    }

    [Fact(DisplayName = "«fast number» is a modifier on the one number type, not a type of its own")]
    public void FastNumberIsAModifierOnTheOneNumberTypeNotATypeOfItsOwn()
    {
        // TYPEHALFRULINGS §1: «fast» is a modifier, so «fast number» is «number»
        // with a representation hint and the checker never sees two number types.
        // The annotation resolves to «number» with nothing to report.
        Assert.Empty(Of("var pace => fast number;\n"));

        // and the modifier rides with the datum rather than the type — it is
        // stripped before the reference the annotation walk reads.
        var datum = Assert.IsType<Ronin.Grammar.Datum>(
            Compilation.Of(new SourceText("var pace => fast number;\n", "Player.ron"))
                       .Module.Scopes[0].Statements[0]);

        Assert.True(datum.Modifiers.Is<Ronin.Lexicon.Fast>());
    }

    [Fact(DisplayName = "an unknown type name is a finding at the annotation")]
    public void AnUnknownTypeNameIsAFindingAtTheAnnotation()
    {
        var finding = Assert.IsType<UnknownType>(Assert.Single(Of("var cash on hand => money;\n")));

        Assert.Equal("money", finding.Name);

        // At the annotation — «money» at column 21 — and not at the declaration.
        Assert.StartsWith("Player.ron:1:21:", Diagnostics.Report(finding));
    }

    [Fact(DisplayName = "an initializer whose type is not the declared one is a finding at the value")]
    public void AnInitializerWhoseTypeIsNotTheDeclaredOneIsAFindingAtTheValue()
    {
        var finding = Assert.IsType<TypeMismatch>(Assert.Single(Of("var x => number = \"text\";\n")));

        Assert.Equal("text", finding.Value);
        Assert.Equal("number", finding.Declared);

        // At the value — «"text"» at column 19 — the half a reader changes more often.
        Assert.StartsWith("Player.ron:1:19:", Diagnostics.Report(finding));

        // A matching initializer is clean, whichever scalar.
        Assert.Empty(Of("var y => text = \"hello\";\n"));
        Assert.Empty(Of("var n => number = 5;\n"));

        // An unknown declared type leaves nothing to compare: its own finding stands
        // and no mismatch is stacked on top.
        Assert.Equal("money", Assert.IsType<UnknownType>(Assert.Single(Of("var cash => money = 5;\n"))).Name);

        // A value whose sort is not inferred yet — a date, no prelude type this pass —
        // is not compared; nor is an untyped datum, which has nothing to compare against.
        Assert.Empty(Of("var day => number = 1984-05-04;\n"));
        Assert.Empty(Of("var loose = 5;\n"));
    }

    [Fact(DisplayName = "a bare type constructor is not a type")]
    public void ABareTypeConstructorIsNotAType()
    {
        // The type is «list of (_)»; «list» alone names none. This is the fixture
        // shape the sweep turns into «list of number».
        Assert.Equal("list", Assert.IsType<UnknownType>(Assert.Single(Of("var items => list;\n"))).Name);
    }

    [Fact(DisplayName = "a parameter and a return type are annotations too")]
    public void AParameterAndAReturnTypeAreAnnotationsToo()
    {
        // Both positions are walked, so both are reported — two sites, two edits.
        var findings = Of("function convert (amount => money) => moolah { return amount; }\n");

        Assert.Equal(["money", "moolah"],
                     findings.OfType<UnknownType>().Select(finding => finding.Name).Order());
        Assert.Equal(2, findings.Count);
    }

    [Fact(DisplayName = "one bad type in two declarations is two findings, one per site")]
    public void OneBadTypeInTwoDeclarationsIsTwoFindingsOnePerSite()
    {
        // Each written annotation is a site of its own — the mistake is reported
        // where it is written, and two declarations are two places to fix.
        var findings = Of("var a => money;\nvar b => money;\n");

        Assert.Equal(2, findings.Count);
        Assert.All(findings, finding => Assert.Equal("money", Assert.IsType<UnknownType>(finding).Name));
    }

    [Fact(DisplayName = "a declared type is usable with no definition")]
    public void ADeclaredTypeIsUsableWithNoDefinition()
    {
        // «type money;» names it; that is enough to annotate with. A definition
        // would give it structure, and an opaque handle you can name and pass but
        // not construct is a real thing rather than an error waiting to happen.
        Assert.Empty(Of("type money;\nvar cash on hand => money;\n"));
    }

    [Fact(DisplayName = "an annotation resolves in its own scope, seeing the types enclosing it")]
    public void AnAnnotationResolvesInItsOwnScopeSeeingTheTypesEnclosingIt()
    {
        // A type declared at module scope is visible in a body nested below it,
        // because the body's table folds the enclosing one in.
        Assert.Empty(Of("type colour;\nfunction paint (with => colour) { return with; }\n"));

        // And a type declared INSIDE a body is not visible to a sibling or the
        // module: the walk resolves each annotation in the scope that owns it, so
        // «shade» is unknown at module scope though a body declares one.
        var finding = Assert.IsType<UnknownType>(Assert.Single(Of("""
                                                                  function mix { type shade; }
                                                                  var background => shade;

                                                                  """)));
        Assert.Equal("shade", finding.Name);
    }

    [Fact(DisplayName = "a whole annotation may be a group, and a keyed group is carried to the checker")]
    public void AWholeAnnotationMayBeAGroupAndAKeyedGroupIsCarriedToTheChecker()
    {
        // A lone round group is a grouped TYPE — «(number)», a grouped function
        // type, or one in a parameter — so type capture reads it as a reference
        // rather than handing it back as the anonymous value a value expression
        // would be. Grouping is load-bearing in type position, so it must reach
        // the resolver from source and not only from a direct resolve.
        Assert.Empty(Of("var x => (number);\n"));
        Assert.Empty(Of("var callback => (text => number);\n"));
        Assert.Empty(Of("function use (callback => (text => number)) { return; }\n"));

        // A keyed round group is admitted and carried, not diagnosed as an unknown
        // type: «optional (a = b)» is grouping the checker will refuse by
        // multiplicity once it exists, per TYPEHALFDECISIONS §3. Clean now; a
        // multiplicity finding when the checker lands.
        Assert.Empty(Of("type a;\ntype b;\nvar x => optional (a = b);\n"));
    }

    [Fact(DisplayName = "an ambiguity inside a keyed group repairs, and does not crash")]
    public void AnAmbiguityInsideAKeyedGroupRepairsAndDoesNotCrash()
    {
        // A keyed group carries a real extent and is walked into by a repair, so an
        // ambiguous arrow in its VALUE or its KEY is a repairable ambiguity at the
        // annotation — not a compiler-terminating source path, which a zero-length
        // node was before the extent was set.
        foreach (var source in (string[])
                 [
                     "type a; type b; type c; type d;\nvar x => optional (a = b => c => d);\n",   // value
                     "var x => optional (text => number => truth = number);\n",                  // key
                     // an unambiguous entry before the ambiguous one — the repair
                     // walk skips the first and finds the divergence in the second
                     "var x => optional (number = text, truth = number => text => truth);\n",
                 ])
        {
            var finding = Assert.IsType<Ambiguous>(Assert.Single(Of(source)));

            Assert.Equal(2, finding.Total);
            Assert.Equal(2, finding.Repairs.Count);
        }
    }

    [Fact(DisplayName = "a chain of arrows is an ambiguity at the annotation, with brackets to repair it")]
    public void AChainOfArrowsIsAnAmbiguityAtTheAnnotationWithBracketsToRepairIt()
    {
        // The arrow does not associate, so a bare chain is a tie the reader
        // brackets — reported with the same finding and repairs a value ambiguity
        // gets, at the annotation rather than at every use.
        const string chained = "var handler => text => number => truth;\n";
        var chain = Assert.IsType<Ambiguous>(Assert.Single(Of(chained)));
        Assert.Equal(2, chain.Total);
        Assert.Equal(2, chain.Repairs.Count);
        foreach (var repair in chain.Repairs) Assert.Empty(Of(Applied(chained, repair)));

        // LOOKUP-ARROW §2: the lookup's arrow and the function arrow compete for
        // the second «=>», so there are THREE bracketings and the finding offers
        // all three — each applying to a clean, uniquely-resolved annotation, and
        // the three distinct. The third, «(lookup text => number) => truth», is the
        // function taking a lookup: an operation whose left is a lookup call, the
        // reading the repair search dropped until it descended a call-shaped
        // competitor's operation.
        const string source = "var table => lookup text => number => truth;\n";
        var lookup = Assert.IsType<Ambiguous>(Assert.Single(Of(source)));

        Assert.Equal(3, lookup.Total);
        Assert.Equal(3, lookup.Repairs.Count);

        var applied = lookup.Repairs.Select(repair => Applied(source, repair)).ToList();

        foreach (var repaired in applied) Assert.Empty(Of(repaired));
        Assert.Equal(3, applied.Distinct().Count());
        Assert.Contains("var table => (lookup text => number) => truth;\n", applied);
    }

    /// <summary>The source with one repair's brackets typed in, right to left.</summary>
    private static string Applied(string source, Repair repair)
    {
        foreach (var insertion in repair.Insertions.OrderByDescending(insertion => insertion.At))
        {
            source = source[..insertion.At] + insertion.Text + source[insertion.At..];
        }

        return source;
    }

    [Fact(DisplayName = "a type member's annotation is read once, in the type's body")]
    public void ATypeMembersAnnotationIsReadOnceInTheTypesBody()
    {
        // The walk stops at a type's «Definition», so a member's annotation is
        // reached by the recursion into the type body and not again at the scope
        // that holds the type. One unknown member type is one finding, not two.
        var finding = Assert.IsType<UnknownType>(Assert.Single(Of("type wallet { var balance => money; }\n")));

        Assert.Equal("money", finding.Name);

        // And a member typed by a name the type itself supplies resolves — the
        // body sees the module scope that declares it.
        Assert.Empty(Of("type currency;\ntype wallet { var held => currency; }\n"));
    }
}
