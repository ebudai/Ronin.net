// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using System.Linq;

namespace Unit;

/// <summary>
///     The semantic type read from a resolved annotation, and its structural
///     equality — the whole of type identity, there being no subtyping.
/// </summary>
[Trait(nameof(Sort), null)]
public class Sorts
{
    private static readonly SymbolTable symbols = new SymbolTable().WithNames(SymbolKind.Type, "Car", "a", "b");

    /// <summary>The sort a resolved annotation names, its user types at the module.</summary>
    private static Sort Of(string annotation)
    {
        new Resolver(symbols, kind: SymbolKind.Type).Resolve(annotation).TryTree(out var tree);

        return Sort.Of(tree, _ => []);
    }

    [Fact(DisplayName = "each well-formed annotation reads as its sort")]
    public void EachWellFormedAnnotationReadsAsItsSort()
    {
        Assert.Equal(new Sort.Scalar("number"), Of("number"));
        Assert.Equal(new Sort.Scalar("text"), Of("text"));
        Assert.Equal(new Sort.Scalar("truth"), Of("truth"));
        Assert.Equal(new Sort.Error(), Of("error"));
        Assert.Equal(new Sort.Named([], "Car"), Of("Car"));

        Assert.Equal(new Sort.List(new Sort.Scalar("number")), Of("list of number"));
        Assert.Equal(new Sort.Optional(new Sort.Scalar("text")), Of("optional text"));
        Assert.Equal(new Sort.Lookup(new Sort.Scalar("text"), new Sort.Scalar("number")), Of("lookup text => number"));

        // A bracketed hole is the type inside it, in an argument and standalone.
        Assert.Equal(new Sort.List(new Sort.Scalar("number")), Of("list of (number)"));
        Assert.Equal(new Sort.Scalar("number"), Of("(number)"));

        // Nesting composes.
        Assert.Equal(new Sort.Optional(new Sort.List(new Sort.Scalar("number"))), Of("optional list of number"));
    }

    [Fact(DisplayName = "a function type carries its parameters, of any number including none")]
    public void AFunctionTypeCarriesItsParametersOfAnyNumberIncludingNone()
    {
        Sort text = new Sort.Scalar("text");
        Sort number = new Sort.Scalar("number");

        Assert.Equal(new Sort.Function([text], number), Of("text => number"));
        Assert.Equal(new Sort.Function([text], number), Of("(text) => number"));
        Assert.Equal(new Sort.Function([text, number], new Sort.Scalar("truth")), Of("(text, number) => truth"));
        Assert.Equal(new Sort.Function([], number), Of("() => number"));
    }

    [Fact(DisplayName = "an arity-wrong group in a type position is no sort")]
    public void AnArityWrongGroupInATypePositionIsNoSort()
    {
        // A keyed group and a multi-part group each stand where one type is wanted;
        // a later pass refuses their arity, and until then they are no single sort.
        Assert.Null(Of("optional (a = b)"));
        Assert.Null(Of("list of (number, text)"));

        // Null propagates through a constructor's holes...
        Assert.Null(Of("lookup (a = b) => number"));
        Assert.Null(Of("lookup number => (a = b)"));

        // ...and through a function's parameter and its result.
        Assert.Null(Of("(a = b) => number"));
        Assert.Null(Of("number => (a = b)"));
    }

    [Fact(DisplayName = "a sort is equal by structure and by nothing else")]
    public void ASortIsEqualByStructureAndByNothingElse()
    {
        Sort number = new Sort.Scalar("number");
        Sort text = new Sort.Scalar("text");

        Assert.Equal(number, new Sort.Scalar("number"));
        Assert.NotEqual(number, text);
        Assert.Equal<Sort>(new Sort.Error(), new Sort.Error());
        Assert.Equal<Sort>(new Sort.Named([], "a"), new Sort.Named([], "a"));
        Assert.NotEqual<Sort>(new Sort.Named([], "a"), new Sort.Named([], "b"));

        // The two no annotation spells: the action type is one of its kind, an
        // inference variable is one by identity.
        Assert.Equal<Sort>(new Sort.Action(), new Sort.Action());
        Assert.NotEqual<Sort>(new Sort.Action(), new Sort.Error());
        Assert.Equal<Sort>(new Sort.Variable(1), new Sort.Variable(1));
        Assert.NotEqual<Sort>(new Sort.Variable(1), new Sort.Variable(2));
        Assert.NotEqual<Sort>(new Sort.Variable(1), number);

        // Cross-kind and non-sort are never equal — a name shared across kinds too.
        Assert.NotEqual<Sort>(new Sort.Scalar("number"), new Sort.Named([], "number"));
        Assert.False(number.Equals("number"));
        Assert.False(number.Equals(null));

        // Constructors — by contents, and «optional» nests.
        Assert.Equal<Sort>(new Sort.List(number), new Sort.List(number));
        Assert.NotEqual<Sort>(new Sort.List(number), new Sort.List(text));
        Assert.Equal<Sort>(new Sort.Optional(number), new Sort.Optional(number));
        Assert.NotEqual<Sort>(new Sort.Optional(new Sort.Optional(number)), new Sort.Optional(number));

        // A lookup's key and value each matter.
        Assert.Equal<Sort>(new Sort.Lookup(text, number), new Sort.Lookup(text, number));
        Assert.NotEqual<Sort>(new Sort.Lookup(text, number), new Sort.Lookup(number, number));
        Assert.NotEqual<Sort>(new Sort.Lookup(text, number), new Sort.Lookup(text, text));

        // A function's result, parameter contents, and parameter count each matter.
        Assert.Equal<Sort>(new Sort.Function([text], number), new Sort.Function([text], number));
        Assert.NotEqual<Sort>(new Sort.Function([text], number), new Sort.Function([text], text));
        Assert.NotEqual<Sort>(new Sort.Function([text], number), new Sort.Function([number], number));
        Assert.NotEqual<Sort>(new Sort.Function([text], number), new Sort.Function([text, text], number));
    }

    [Fact(DisplayName = "equal sorts hash alike")]
    public void EqualSortsHashAlike()
    {
        Sort number = new Sort.Scalar("number");
        Sort text = new Sort.Scalar("text");

        Assert.Equal(new Sort.Scalar("number").GetHashCode(), number.GetHashCode());
        Assert.Equal(new Sort.Error().GetHashCode(), new Sort.Error().GetHashCode());
        Assert.Equal(new Sort.Named(["m"], "a").GetHashCode(), new Sort.Named(["m"], "a").GetHashCode());
        Assert.Equal(new Sort.List(number).GetHashCode(), new Sort.List(number).GetHashCode());
        Assert.Equal(new Sort.Optional(number).GetHashCode(), new Sort.Optional(number).GetHashCode());
        Assert.Equal(new Sort.Lookup(text, number).GetHashCode(), new Sort.Lookup(text, number).GetHashCode());
        Assert.Equal(new Sort.Function([text], number).GetHashCode(), new Sort.Function([text], number).GetHashCode());
        Assert.Equal(new Sort.Action().GetHashCode(), new Sort.Action().GetHashCode());
        Assert.Equal(new Sort.Variable(1).GetHashCode(), new Sort.Variable(1).GetHashCode());
    }

    [Fact(DisplayName = "the compilation keeps each resolved annotation's sort, and no arity-wrong one")]
    public void TheCompilationKeepsEachResolvedAnnotationsSort()
    {
        var kept = Compilation.Of(new SourceText("type Car;\nvar x => list of number;\nvar y => Car;\n", "s.ron"));

        Assert.Empty(kept.Findings);
        Assert.Equal(new Sort[] { new Sort.List(new Sort.Scalar("number")), new Sort.Named(["s.ron"], "Car") },
                     kept.Types.Select(annotation => annotation.Type));

        // An arity-wrong annotation is kept with a null sort, its span still recorded.
        var arity = Compilation.Of(new SourceText("type a;\ntype b;\nvar m => optional (a = b);\n", "s.ron"));

        Assert.Empty(arity.Findings);
        Assert.Null(Assert.Single(arity.Types).Type);

        // A too-long annotation resolves to no tree: reported, and no sort fabricated,
        // rather than silently vanishing where a later pass could not tell it from one
        // that was never written.
        var chain = string.Concat(Enumerable.Repeat("optional ", Resolver.MaxLexemes + 1));
        var huge = Compilation.Of(new SourceText($"var z => {chain}number;\n", "s.ron"));

        Assert.Empty(huge.Types);
        Assert.IsType<OversizeType>(Assert.Single(huge.Findings));
    }

    [Fact(DisplayName = "two same-named types in two functions are two distinct sorts")]
    public void TwoSameNamedTypesInTwoFunctionsAreTwoDistinctSorts()
    {
        // REAUDIT54 finding 1, the witness: «token» in «left» and «token» in «right»
        // are two opaque types, told apart by their declaring container and not by a
        // spelling they share. Under the old identity they compared equal.
        var compilation = Compilation.Of(new SourceText(
            "function left { type token; var x => token; }\nfunction right { type token; var y => token; }\n", "s.ron"));

        Assert.Empty(compilation.Findings);

        var named = compilation.Types.Select(annotation => annotation.Type).OfType<Sort.Named>().ToArray();

        Assert.Equal(2, named.Length);
        Assert.NotEqual<Sort>(named[0], named[1]);
    }

    [Fact(DisplayName = "two same-named types in two modules are two distinct sorts")]
    public void TwoSameNamedTypesInTwoModulesAreTwoDistinctSorts()
    {
        // CONTAINER-IDENTITY-RULING §1 / NAMEDIDENTITY Q1b: the module is the root of
        // the container, so «token» in two files is two types even at module level —
        // the case a debug-is-development session hits comparing a file's types
        // before an edit to the same file's after, which is a comparison across
        // compilations.
        Sort left = Assert.Single(Compilation.Of(
            new SourceText("type token; var x => token;\n", "left.ron")).Types).Type;
        Sort right = Assert.Single(Compilation.Of(
            new SourceText("type token; var x => token;\n", "right.ron")).Types).Type;

        Assert.NotEqual(left, right);
    }

    [Fact(DisplayName = "a type declared in a block belongs to its container, nameable and identified there")]
    public void ATypeDeclaredInABlockBelongsToItsContainer()
    {
        // SCOPE-IDENTITY-RULING H, wide: «type X;» has no runtime lifetime, so it is
        // not block-scoped — it belongs to the nearest named container, nameable
        // throughout it and identified by it. Here two types are declared two blocks
        // deep and used in the function body outside both, which resolves, and their
        // sort is the container's, not the block's.
        var compilation = Compilation.Of(new SourceText(
            "function f { { { type token; type other; } } var x => token; var y => other; }\n", "s.ron"));

        Assert.Empty(compilation.Findings);

        var named = compilation.Types.Select(annotation => annotation.Type).OfType<Sort.Named>().ToArray();

        Assert.Equal(["token", "other"], named.Select(sort => sort.Name));
        Assert.All(named, sort => Assert.Equal(["s.ron", "f"], sort.Container));
    }

    [Fact(DisplayName = "two same-named types in one function are a duplicate, wherever in it they sit")]
    public void TwoSameNamedTypesInOneFunctionAreADuplicate()
    {
        // The other half of the same ruling: a type name is unique within its named
        // container, across its transparent sub-scopes. Two «token»s in two blocks of
        // one function name one type twice — no cue tells them apart — so the second
        // is «Shadowed», where block scoping made them two distinct types.
        var compilation = Compilation.Of(new SourceText(
            "function f { { type token; } { type token; } }\n", "s.ron"));

        Assert.Equal(FindingKind.Shadowed, Assert.Single(compilation.Findings).Kind);
    }

    [Fact(DisplayName = "gathering a container's block-level types stops at a named scope nested in a block")]
    public void GatheringStopsAtANamedScopeNestedInABlock()
    {
        // A function nested in a block is a named container of its own; the gather
        // that lifts a container's block-level types to it does not reach into the
        // nested one — its types are its own, so «g»'s (there are none) do not become
        // «f»'s, and this compiles clean.
        Assert.Empty(Compilation.Of(new SourceText("function f { { function g { } } }\n", "s.ron")).Findings);
    }

    [Fact(DisplayName = "a container-level type collision blames the later declaration, in either order")]
    public void AContainerLevelTypeCollisionBlamesTheLaterDeclaration()
    {
        // REAUDIT55 finding 5: gathering a container's block-level types must not
        // reorder them. A collision blames the LATER declaration — the one a reader
        // changes — and names the earlier as first, whichever way the block and the
        // direct declaration are written.
        foreach (var source in new[]
                 {
                     "function f { { type token; } type token; }\n",
                     "function f { type token; { type token; } }\n",
                 })
        {
            var finding = Assert.Single(Compilation.Of(new SourceText(source, "p.ron")).Findings);

            Assert.Equal(FindingKind.Shadowed, finding.Kind);
            Assert.True(finding.Primary.Offset > Assert.Single(finding.Related).Span.Offset);
        }
    }

    [Fact(DisplayName = "a signature carries the sort each of its spellings resolves to, parameters and return")]
    public void ASignatureCarriesTheSortEachOfItsSpellingsResolvesTo()
    {
        // REAUDIT55 finding 3: the signature stores the resolved parameter and return
        // SORTS beside their spellings, so a later checker unifies them without
        // resolving the words a second time.
        var declarations = Compilation.Of(new SourceText(
            "function area of (r => number) => text { return r; }\n", "s.ron")).Declarations;

        var signature = declarations.Overloads.Values.Single().Single();

        Assert.Equal([[new Sort.Scalar("number")]], signature.ParameterSorts);
        Assert.Equal(new Sort.Scalar("text"), signature.ReturnSort);

        // A parameter or return the words are no one type keeps a null sort in its
        // slot — the spelling stays, and the classifier falls back to it.
        var untyped = Compilation.Of(new SourceText("function ping (x) { }\n", "s.ron")).Declarations;
        var pinged = untyped.Overloads.Values.Single().Single();

        Assert.Null(Assert.Single(Assert.Single(pinged.ParameterSorts)));
        Assert.Null(pinged.ReturnSort);
    }

    [Fact(DisplayName = "a type declared in two bodies of one overloaded shape is shadowed")]
    public void ATypeDeclaredInTwoBodiesOfOneOverloadedShapeIsShadowed()
    {
        // CONTAINER-IDENTITY-RULING §2 (B): the bodies of one overloaded shape are
        // one container, so «token» declared in both is «Shadowed» — H's uniqueness
        // across the bodies that share a name, not only within one. The later is
        // blamed, and a block-nested type counts as its shape's, not the block's.
        var shadowed = Assert.Single(Compilation.Of(new SourceText(
            "function use (x => number) { type token; return x; }\n" +
            "function use (x => text) { type token; return x; }\n", "p.ron")).Findings,
            finding => finding.Kind is FindingKind.Shadowed);

        Assert.Contains("in another body of this shape", shadowed.Message);
        Assert.True(shadowed.Primary.Offset > Assert.Single(shadowed.Related).Span.Offset);

        Assert.Contains(Compilation.Of(new SourceText(
            "function use (x => number) { { type token; } return x; }\n" +
            "function use (x => text) { type token; return x; }\n", "p.ron")).Findings,
            finding => finding.Kind is FindingKind.Shadowed);

        // Different type names across the bodies collide over nothing — and one body
        // declaring no type at all is no collision either.
        Assert.DoesNotContain(Compilation.Of(new SourceText(
            "function use (x => number) { type box; return x; }\n" +
            "function use (x => text) { type crate; return x; }\n", "p.ron")).Findings,
            finding => finding.Kind is FindingKind.Shadowed);

        Assert.DoesNotContain(Compilation.Of(new SourceText(
            "function use (x => number) { type box; return x; }\n" +
            "function use (x => text) { return x; }\n", "p.ron")).Findings,
            finding => finding.Kind is FindingKind.Shadowed);
    }

    [Fact(DisplayName = "a type in a parameter-default delegate counts toward overload-wide uniqueness")]
    public void ATypeInAParameterDefaultDelegateCountsTowardOverloadWideUniqueness()
    {
        // REAUDIT56 finding 3: the cross-body check must count a shape's COMPLETE
        // declaration set — body, transparent body scopes, and ancillary parameter-
        // default delegates — since all belong to the one container under B.
        // ancillary/ancillary: «token» in each overload's callback delegate, later blamed.
        var collision = Assert.Single(Compilation.Of(new SourceText(
            "function use (x => number) with (callback = (y) => { type token; return y; }) { return x; }\n" +
            "function use (x => text) with (callback = (y) => { type token; return y; }) { return x; }\n", "p.ron")).Findings,
            finding => finding.Kind is FindingKind.Shadowed);

        Assert.True(collision.Primary.Offset > Assert.Single(collision.Related).Span.Offset);

        // body/ancillary, and a block-nested type within a delegate: «token» direct in
        // one body collides with one block-nested in the other's delegate.
        Assert.Contains(Compilation.Of(new SourceText(
            "function use (x => number) with (callback = (y) => { return y; }) { type token; return x; }\n" +
            "function use (x => text) with (callback = (y) => { { type token; } return y; }) { return x; }\n", "p.ron")).Findings,
            finding => finding.Kind is FindingKind.Shadowed);

        // A different name across the delegates collides over nothing.
        Assert.DoesNotContain(Compilation.Of(new SourceText(
            "function use (x => number) with (callback = (y) => { type box; return y; }) { return x; }\n" +
            "function use (x => text) with (callback = (y) => { type crate; return y; }) { return x; }\n", "p.ron")).Findings,
            finding => finding.Kind is FindingKind.Shadowed);
    }

    [Fact(DisplayName = "a type in a parameter-default delegate belongs to the enclosing function, not the module")]
    public void ATypeInAParameterDefaultDelegateBelongsToTheEnclosingFunction()
    {
        // REAUDIT55 finding 2: a parameter-default delegate is transparent, so a type
        // it declares belongs to the enclosing function (H) — the stop at the named
        // construct is the whole of it, signature included. The function's body and
        // the delegate may both name «token»; the module outside the function may not.
        var compilation = Compilation.Of(new SourceText(
            "function run (callback = (x) => { type token; var local => token; return x; }) " +
            "{ var inside => token; return 1; }\n" +
            "var outside => token;\n", "m.ron"));

        // Only «outside» is unknown; «inside» (the body) and «local» (the delegate) resolve.
        var unknown = Assert.Single(compilation.Findings);

        Assert.Equal(FindingKind.UnknownType, unknown.Kind);
        Assert.EndsWith("2:16", unknown.Primary.ToString());

        // Both resolved «token»s carry the function's container, not the module's.
        Assert.All(compilation.Types.Where(annotation => annotation.Type is Sort.Named),
                   annotation => Assert.Equal(["m.ron", "run"], ((Sort.Named)annotation.Type).Container));
    }

    [Fact(DisplayName = "a function's own signature sees the types its container declares")]
    public void AFunctionsOwnSignatureSeesTheTypesItsContainerDeclares()
    {
        // REAUDIT56 finding 2: a datatype is usable THROUGHOUT its container, the
        // signature included, so a parameter or return annotation naming a body-local
        // type resolves against the function's own table — not the scope that declares
        // it, where the type is invisible. Both «token»s carry the function container.
        var direct = Compilation.Of(new SourceText(
            "function run (value => token) => token { type token; return value; }\n", "m.ron"));

        Assert.DoesNotContain(direct.Findings, finding => finding.Kind is FindingKind.UnknownType);
        Assert.Equal(2, direct.Types.Count(annotation => annotation.Type is Sort.Named));
        Assert.All(direct.Types.Where(annotation => annotation.Type is Sort.Named),
                   annotation => Assert.Equal(["m.ron", "run"], ((Sort.Named)annotation.Type).Container));

        // And the sorts stored on the signature itself resolve there too, no longer
        // null — the parameter and the return each the function-local «token».
        var signature = direct.Declarations.Overloads.Values.Single().Single();

        Assert.Equal([[new Sort.Named(["m.ron", "run"], "token")]], signature.ParameterSorts);
        Assert.Equal(new Sort.Named(["m.ron", "run"], "token"), signature.ReturnSort);

        // The same when the type is declared in a parameter-default delegate and named
        // by a sibling parameter and the return.
        var ancillary = Compilation.Of(new SourceText(
            "function run (callback = (x) => { type token; return x; }) with (value => token) => token " +
            "{ return value; }\n", "m.ron"));

        Assert.DoesNotContain(ancillary.Findings, finding => finding.Kind is FindingKind.UnknownType);

        // A signature type that truly is nowhere is still «UnknownType», parameter and return.
        var undeclared = Compilation.Of(new SourceText(
            "function run (value => nope) => nope { return value; }\n", "m.ron"));

        Assert.Equal(2, undeclared.Findings.Count(finding => finding.Kind is FindingKind.UnknownType));
    }

    [Fact(DisplayName = "an inference variable owns a requirement slot the constraint pass will fill")]
    public void AnInferenceVariableOwnsARequirementSlot()
    {
        // Q1 / REAUDIT55 finding 4: «Variable» has a place for the inferred
        // requirement set now — owned on the variable, empty until the constraint
        // pass records into it. Filling it does not change which variable it is,
        // because identity, not the requirements, is the whole of equality.
        var variable = new Sort.Variable(1);

        Assert.Empty(variable.Requirements);

        variable.Requirements.Add(Pattern.Parse("print _"));

        Assert.Single(variable.Requirements);
        Assert.Equal<Sort>(new Sort.Variable(1), variable);
    }
}
