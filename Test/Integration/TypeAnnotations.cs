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

    [Fact(DisplayName = "a return whose value is not the declared return type is a finding at the value")]
    public void AReturnWhoseValueIsNotTheDeclaredReturnTypeIsAFindingAtTheValue()
    {
        var finding = Assert.IsType<TypeMismatch>(Assert.Single(Of("function m => number { return \"text\"; }\n")));

        Assert.Equal("text", finding.Value);
        Assert.Equal("number", finding.Declared);

        // At the returned value — «"text"» at column 31 — not at «return» or the type.
        Assert.StartsWith("Player.ron:1:31:", Diagnostics.Report(finding));

        // A matching return is clean, whichever scalar.
        Assert.Empty(Of("function s => text { return \"text\"; }\n"));
        Assert.Empty(Of("function n => number { return 5; }\n"));

        // A function with no written return type infers it later — nothing to compare.
        Assert.Empty(Of("function g { return 5; }\n"));

        // An unknown return type leaves only its own annotation finding, no mismatch on top.
        Assert.Equal("money", Assert.IsType<UnknownType>(Assert.Single(Of("function h => money { return 5; }\n"))).Name);

        // A return that names a local datum is read against the return type — the
        // mismatch found at the name, «s» at column 46.
        var named = Assert.IsType<TypeMismatch>(Assert.Single(Of("function f => number { var s => text; return s; }\n")));
        Assert.Equal("text", named.Value);
        Assert.Equal("number", named.Declared);
        Assert.StartsWith("Player.ron:1:46:", Diagnostics.Report(named));

        // A return that names a typed parameter reads against it too — a match is clean,
        // a mismatch a finding at the name.
        Assert.Empty(Of("function p (x => number) => number { return x; }\n"));
        var parameter = Assert.IsType<TypeMismatch>(Assert.Single(Of("function q (x => text) => number { return x; }\n")));
        Assert.Equal("text", parameter.Value);
        Assert.Equal("number", parameter.Declared);

        // An untyped parameter is generic — nothing to compare; and a return that did
        // not resolve is left to its own walk.
        Assert.Empty(Of("function p (x) => number { return x; }\n"));
        Assert.Empty(Of("function f => number { return nope; }\n"));

        // A return reads a non-scalar answer the same way an initializer does — its sort
        // spelled whole, a match clean.
        var whole = Assert.IsType<TypeMismatch>(Assert.Single(
            Of("function m => number { var xs => list of number; return xs; }\n")));
        Assert.Equal("list of number", whole.Value);
        Assert.Empty(Of("function m => list of number { var xs => list of number; return xs; }\n"));

        // An answer whose sort has no spelling — the bottom «error» — is left uncompared.
        Assert.Empty(Of("function m => number { var e => error; return e; }\n"));
    }

    [Fact(DisplayName = "a list element whose type is not the declared element type is a finding at the element")]
    public void AListElementWhoseTypeIsNotTheDeclaredElementTypeIsAFindingAtTheElement()
    {
        var finding = Assert.IsType<TypeMismatch>(Assert.Single(Of("var xs => list of number = [\"text\"];\n")));

        Assert.Equal("text", finding.Value);
        Assert.Equal("number", finding.Declared);

        // At the element — «"text"» at column 29 — the value declared «number».
        Assert.StartsWith("Player.ron:1:29:", Diagnostics.Report(finding));

        // One bad element among good ones is one finding, at that element.
        var mixed = Assert.IsType<TypeMismatch>(Assert.Single(Of("var ws => list of number = [1, \"text\", 3];\n")));
        Assert.Equal("text", mixed.Value);

        // A matching list and an empty one are clean.
        Assert.Empty(Of("var ys => list of number = [1, 2, 3];\n"));
        Assert.Empty(Of("var zs => list of number = [];\n"));

        // A list where a scalar is declared is a whole-value mismatch, at the
        // declaration: a list is «list of» something and never a «number».
        var whole = Assert.IsType<TypeMismatch>(Assert.Single(Of("var scalar => number = [1];\n")));
        Assert.Equal("list", whole.Value);
        Assert.Equal("number", whole.Declared);
        Assert.StartsWith("Player.ron:1:5:", Diagnostics.Report(whole));   // at «scalar»

        // A lookup where a list is declared is a whole-value mismatch — a lookup is not
        // a list, whatever its entries.
        var kind = Assert.IsType<TypeMismatch>(Assert.Single(Of("var ks => list of number = [\"k\" = 1];\n")));
        Assert.Equal("lookup", kind.Value);
        Assert.Equal("list of number", kind.Declared);

        // Deferred: a non-literal element — a date — read as no scalar this pass.
        Assert.Empty(Of("var ds => list of number = [1984-05-04];\n"));
    }

    [Fact(DisplayName = "a lookup entry's key or value that is not the declared one is a finding at that entry")]
    public void ALookupEntrysKeyOrValueThatIsNotTheDeclaredOneIsAFindingAtThatEntry()
    {
        // Key «text», value «number» — a clean match.
        Assert.Empty(Of("var m => lookup text => number = [\"k\" = 1];\n"));

        // A value of the wrong type is a finding, its declared type the value type.
        var value = Assert.IsType<TypeMismatch>(Assert.Single(Of("var m => lookup text => number = [\"k\" = \"x\"];\n")));
        Assert.Equal("text", value.Value);
        Assert.Equal("number", value.Declared);

        // A key of the wrong type is a finding, its declared type the key type.
        var key = Assert.IsType<TypeMismatch>(Assert.Single(Of("var m => lookup text => number = [1 = 2];\n")));
        Assert.Equal("number", key.Value);
        Assert.Equal("text", key.Declared);

        // A lookup where a scalar is declared is a whole-value mismatch at the declaration.
        var whole = Assert.IsType<TypeMismatch>(Assert.Single(Of("var s => number = [\"k\" = 1];\n")));
        Assert.Equal("lookup", whole.Value);
        Assert.Equal("number", whole.Declared);

        // A mixed collection is a parse error, not a type one.
        Assert.DoesNotContain(Of("var mixed => lookup text => number = [1, \"a\" = 2];\n"), f => f is TypeMismatch);
    }

    [Fact(DisplayName = "an initializer that names a value of another type is a finding at the name")]
    public void AnInitializerThatNamesAValueOfAnotherTypeIsAFindingAtTheName()
    {
        var finding = Assert.IsType<TypeMismatch>(Assert.Single(Of("var name => text;\nvar age => number = name;\n")));

        Assert.Equal("text", finding.Value);
        Assert.Equal("number", finding.Declared);

        // At the reference «name» on line 2, column 21 — not at «age» or the type.
        Assert.StartsWith("Player.ron:2:21:", Diagnostics.Report(finding));

        // A reference whose type agrees is clean.
        Assert.Empty(Of("var greeting => text;\nvar message => text = greeting;\n"));

        // A parameter is a name this scope holds a sort for too, so a value that names
        // one of the wrong type is a finding as much as a datum reference is.
        var parameter = Assert.IsType<TypeMismatch>(Assert.Single(
            Of("function f (x => text) => number { var y => number = x; return 5; }\n")));
        Assert.Equal("text", parameter.Value);
        Assert.Equal("number", parameter.Declared);

        // An untyped parameter is generic — no sort to read — and a name that does not
        // resolve at all is left to its own walk; neither is compared.
        Assert.Empty(Of("function f (x) => number { var y => number = x; return 5; }\n"));
        Assert.Empty(Of("var y => number = nope;\n"));

        // A reference to a non-scalar value renders its sort in the finding.
        var list = Assert.IsType<TypeMismatch>(Assert.Single(Of("var xs => list of number;\nvar y => number = xs;\n")));
        Assert.Equal("list of number", list.Value);
        Assert.Equal("number", list.Declared);
    }

    [Fact(DisplayName = "a non-scalar value's sort is spelled out in the finding")]
    public void ANonScalarValuesSortIsSpelledOutInTheFinding()
    {
        // A list, an optional, a lookup — each spelled as its annotation would be.
        Assert.Equal("list of number", Assert.IsType<TypeMismatch>(Assert.Single(
            Of("var xs => list of number;\nvar y => number = xs;\n"))).Value);
        Assert.Equal("optional text", Assert.IsType<TypeMismatch>(Assert.Single(
            Of("var o => optional text;\nvar y => number = o;\n"))).Value);
        Assert.Equal("lookup text => number", Assert.IsType<TypeMismatch>(Assert.Single(
            Of("var m => lookup text => number;\nvar y => number = m;\n"))).Value);

        // Nested, spelled by recursion.
        Assert.Equal("list of list of number", Assert.IsType<TypeMismatch>(Assert.Single(
            Of("var xss => list of list of number;\nvar y => number = xss;\n"))).Value);

        // A matching non-scalar reference is clean — no spelling needed.
        Assert.Empty(Of("var xs => list of number;\nvar ys => list of number = xs;\n"));

        // A sort this pass does not spell — the bottom «error», and any aggregate
        // carrying one — is left uncompared rather than half-spelled.
        Assert.Empty(Of("var le => list of error;\nvar y => number = le;\n"));
        Assert.Empty(Of("var oe => optional error;\nvar y => number = oe;\n"));
        Assert.Empty(Of("var mk => lookup error => number;\nvar y => number = mk;\n"));
        Assert.Empty(Of("var mv => lookup text => error;\nvar y => number = mv;\n"));
    }

    [Fact(DisplayName = "a function-typed or named value's sort is spelled too")]
    public void AFunctionTypedOrNamedValuesSortIsSpelledToo()
    {
        // A function type, its parameters however many, its result recursed into.
        Assert.Equal("text => number", Assert.IsType<TypeMismatch>(Assert.Single(
            Of("var fn => text => number;\nvar y => number = fn;\n"))).Value);
        Assert.Equal("() => number", Assert.IsType<TypeMismatch>(Assert.Single(
            Of("var fn => () => number;\nvar y => number = fn;\n"))).Value);
        Assert.Equal("(text, number) => truth", Assert.IsType<TypeMismatch>(Assert.Single(
            Of("var fn => (text, number) => truth;\nvar y => number = fn;\n"))).Value);

        // A named type, by its name; a match is clean.
        Assert.Equal("currency", Assert.IsType<TypeMismatch>(Assert.Single(
            Of("type currency;\nvar held => currency;\nvar y => number = held;\n"))).Value);
        Assert.Empty(Of("type currency;\nvar held => currency;\nvar y => currency = held;\n"));

        // A function whose parameter or result has no spelling is left uncompared,
        // not half-named.
        Assert.Empty(Of("var fn => error => number;\nvar y => number = fn;\n"));
        Assert.Empty(Of("var fn => text => error;\nvar y => number = fn;\n"));
    }

    [Fact(DisplayName = "a call is read against the declaration through its callee's return type")]
    public void ACallIsReadAgainstTheDeclarationThroughItsCalleesReturnType()
    {
        // A call whose return type is not the declared one is a finding, at the call.
        var call = Assert.IsType<TypeMismatch>(Assert.Single(
            Of("function double (x => number) => number { return x; }\nvar y => text = double 5;\n")));
        Assert.Equal("number", call.Value);
        Assert.Equal("text", call.Declared);

        // A match is clean.
        Assert.Empty(Of("function double (x => number) => number { return x; }\nvar y => number = double 5;\n"));

        // The return sort is spelled whole, non-scalar and all.
        Assert.Equal("list of number", Assert.IsType<TypeMismatch>(Assert.Single(
            Of("function pair (x => number) => list of number { return [x]; }\nvar y => number = pair 5;\n"))).Value);

        // A call reads the same way in return position and as a collection element.
        Assert.IsType<TypeMismatch>(Assert.Single(
            Of("function double (x => number) => number { return x; }\nfunction m => text { return double 5; }\n")));
        Assert.IsType<TypeMismatch>(Assert.Single(
            Of("function double (x => number) => number { return x; }\nvar xs => list of text = [double 5];\n")));

        // A callee whose return type is inferred rather than written is deferred, its
        // return sort not yet known — a later slice.
        Assert.Empty(Of("function id (x => number) { return x; }\nvar y => text = id 5;\n"));
    }

    [Fact(DisplayName = "a call's argument is read against the type its parameter takes")]
    public void ACallsArgumentIsReadAgainstTheTypeItsParameterTakes()
    {
        // An argument of the wrong type is a finding, at the argument.
        var arg = Assert.IsType<TypeMismatch>(Assert.Single(
            Of("function double (x => number) => number { return x; }\nvar y => number = double \"text\";\n")));
        Assert.Equal("text", arg.Value);
        Assert.Equal("number", arg.Declared);

        // A match is clean.
        Assert.Empty(Of("function double (x => number) => number { return x; }\nvar y => number = double 5;\n"));

        // A parameter across blocks lines up with the argument that fills it — «to»'s
        // argument against «to»'s parameter — and a non-scalar parameter is spelled whole.
        Assert.Equal("text", Assert.IsType<TypeMismatch>(Assert.Single(
            Of("function add (x => number) to (y => number) => number { return x; }\nvar z => number = add 1 to \"text\";\n"))).Value);
        Assert.Equal("list of number", Assert.IsType<TypeMismatch>(Assert.Single(
            Of("function head (xs => list of number) => number { return 0; }\nvar z => number = head 5;\n"))).Declared);

        // An argument to a call in any position is read — a return here.
        Assert.IsType<TypeMismatch>(Assert.Single(
            Of("function double (x => number) => number { return x; }\nfunction m => number { return double \"text\"; }\n")));

        // Left uncompared: a generic parameter (no type), an argument whose sort is not
        // inferred (a date), and an argument or a parameter with no spelling (the bottom «error»).
        Assert.Empty(Of("function id (x) => number { return 0; }\nvar z => number = id \"text\";\n"));
        Assert.Empty(Of("function double (x => number) => number { return x; }\nvar z => number = double 1984-05-04;\n"));
        Assert.Empty(Of("function double (x => number) => number { return x; }\nvar e => error;\nvar z => number = double e;\n"));
        Assert.Empty(Of("function f (x => error) => number { return 0; }\nvar z => number = f 5;\n"));
    }

    [Fact(DisplayName = "a nested aggregate literal is checked to its leaves")]
    public void ANestedAggregateLiteralIsCheckedToItsLeaves()
    {
        // A list of lists matches all the way down.
        Assert.Empty(Of("var ns => list of list of number = [[1], [2, 3]];\n"));

        // A wrong leaf, however deep, is a finding at the leaf.
        var deep = Assert.IsType<TypeMismatch>(Assert.Single(Of("var ns => list of list of number = [[\"text\"]];\n")));
        Assert.Equal("text", deep.Value);
        Assert.Equal("number", deep.Declared);

        // A lookup whose value is itself an aggregate is checked into it — a match clean,
        // a wrong leaf a finding.
        Assert.Empty(Of("var m => lookup text => list of number = [\"k\" = [1]];\n"));
        var value = Assert.IsType<TypeMismatch>(Assert.Single(
            Of("var m => lookup text => list of number = [\"k\" = [\"x\"]];\n")));
        Assert.Equal("text", value.Value);
        Assert.Equal("number", value.Declared);

        // A collection nested where a scalar element or entry is expected is deferred:
        // within a nesting there is no declaration to point the whole-kind mismatch at.
        Assert.Empty(Of("var xs => list of number = [[1]];\n"));
        Assert.Empty(Of("var mm => lookup text => number = [\"k\" = [\"j\" = 1]];\n"));

        // An entry type this pass does not spell is left uncompared, not half-named.
        Assert.Empty(Of("var le => list of error = [5];\n"));
    }

    [Fact(DisplayName = "an empty collection takes its kind from what is expected of it")]
    public void AnEmptyCollectionTakesItsKindFromWhatIsExpectedOfIt()
    {
        // Where a list or a lookup is declared, «[]» agrees — an empty one of that kind.
        Assert.Empty(Of("var xs => list of number = [];\n"));
        Assert.Empty(Of("var m => lookup text => number = [];\n"));

        // Where neither is, «[]» is the empty list it defaults to, and that is a mismatch
        // at the declaration.
        var scalar = Assert.IsType<TypeMismatch>(Assert.Single(Of("var y => number = [];\n")));
        Assert.Equal("list", scalar.Value);
        Assert.Equal("number", scalar.Declared);
        Assert.StartsWith("Player.ron:1:5:", Diagnostics.Report(scalar));   // at «y»

        var optional = Assert.IsType<TypeMismatch>(Assert.Single(Of("var o => optional text = [];\n")));
        Assert.Equal("list", optional.Value);
        Assert.Equal("optional text", optional.Declared);

        // Nested, «[]» is read the same way: an empty inner list is clean where a list is
        // the element or entry type, and deferred where a scalar is — nothing to point at.
        Assert.Empty(Of("var ns => list of list of number = [[]];\n"));
        Assert.Empty(Of("var mm => lookup text => list of number = [\"k\" = []];\n"));
        Assert.Empty(Of("var xs => list of number = [[]];\n"));
    }

    [Fact(DisplayName = "a name from an enclosing scope is read against the sort declared there")]
    public void ANameFromAnEnclosingScopeIsReadAgainstTheSortDeclaredThere()
    {
        // A return that names a module-level datum reaches outward to it.
        var datum = Assert.IsType<TypeMismatch>(Assert.Single(
            Of("var outer => text;\nfunction f => number { return outer; }\n")));
        Assert.Equal("text", datum.Value);
        Assert.Equal("number", datum.Declared);

        // A match is clean.
        Assert.Empty(Of("var outer => number;\nfunction f => number { return outer; }\n"));

        // An initializer reaching an enclosing name reads against it too.
        var initializer = Assert.IsType<TypeMismatch>(Assert.Single(
            Of("var outer => text;\nfunction f => number { var y => number = outer; return 5; }\n")));
        Assert.Equal("text", initializer.Value);
        Assert.Equal("number", initializer.Declared);

        // An enclosing scope's parameter reaches an inner body,
        var enclosingParameter = Assert.IsType<TypeMismatch>(Assert.Single(
            Of("function g (p => text) => number { function h => number { return p; } return 5; }\n")));
        Assert.Equal("text", enclosingParameter.Value);

        // and the reach crosses more than one scope.
        var twoDeep = Assert.IsType<TypeMismatch>(Assert.Single(
            Of("var top => text;\nfunction a => number { function b => number { return top; } return 5; }\n")));
        Assert.Equal("text", twoDeep.Value);
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
