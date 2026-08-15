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

        return Sort.Of(tree, _ => string.Empty);
    }

    [Fact(DisplayName = "each well-formed annotation reads as its sort")]
    public void EachWellFormedAnnotationReadsAsItsSort()
    {
        Assert.Equal(new Sort.Scalar("number"), Of("number"));
        Assert.Equal(new Sort.Scalar("text"), Of("text"));
        Assert.Equal(new Sort.Scalar("truth"), Of("truth"));
        Assert.Equal(new Sort.Error(), Of("error"));
        Assert.Equal(new Sort.Named("", "Car"), Of("Car"));

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
        Assert.Equal<Sort>(new Sort.Named("", "a"), new Sort.Named("", "a"));
        Assert.NotEqual<Sort>(new Sort.Named("", "a"), new Sort.Named("", "b"));

        // The two no annotation spells: the action type is one of its kind, an
        // inference variable is one by identity.
        Assert.Equal<Sort>(new Sort.Action(), new Sort.Action());
        Assert.NotEqual<Sort>(new Sort.Action(), new Sort.Error());
        Assert.Equal<Sort>(new Sort.Variable(1), new Sort.Variable(1));
        Assert.NotEqual<Sort>(new Sort.Variable(1), new Sort.Variable(2));
        Assert.NotEqual<Sort>(new Sort.Variable(1), number);

        // Cross-kind and non-sort are never equal — a name shared across kinds too.
        Assert.NotEqual<Sort>(new Sort.Scalar("number"), new Sort.Named("", "number"));
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
        Assert.Equal(new Sort.Named("", "a").GetHashCode(), new Sort.Named("", "a").GetHashCode());
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
        Assert.Equal(new Sort[] { new Sort.List(new Sort.Scalar("number")), new Sort.Named("", "Car") },
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
        Assert.All(named, sort => Assert.Equal("/f", sort.Container));
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
