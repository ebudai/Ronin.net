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

    /// <summary>The sort a resolved annotation names.</summary>
    private static Sort Of(string annotation)
    {
        new Resolver(symbols, kind: SymbolKind.Type).Resolve(annotation).TryTree(out var tree);

        return Sort.Of(tree);
    }

    [Fact(DisplayName = "each well-formed annotation reads as its sort")]
    public void EachWellFormedAnnotationReadsAsItsSort()
    {
        Assert.Equal(new Sort.Scalar("number"), Of("number"));
        Assert.Equal(new Sort.Scalar("text"), Of("text"));
        Assert.Equal(new Sort.Scalar("truth"), Of("truth"));
        Assert.Equal(new Sort.Error(), Of("error"));
        Assert.Equal(new Sort.Named("Car"), Of("Car"));

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
        Assert.Equal<Sort>(new Sort.Named("a"), new Sort.Named("a"));
        Assert.NotEqual<Sort>(new Sort.Named("a"), new Sort.Named("b"));

        // Cross-kind and non-sort are never equal — a name shared across kinds too.
        Assert.NotEqual<Sort>(new Sort.Scalar("number"), new Sort.Named("number"));
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
        Assert.Equal(new Sort.Named("a").GetHashCode(), new Sort.Named("a").GetHashCode());
        Assert.Equal(new Sort.List(number).GetHashCode(), new Sort.List(number).GetHashCode());
        Assert.Equal(new Sort.Optional(number).GetHashCode(), new Sort.Optional(number).GetHashCode());
        Assert.Equal(new Sort.Lookup(text, number).GetHashCode(), new Sort.Lookup(text, number).GetHashCode());
        Assert.Equal(new Sort.Function([text], number).GetHashCode(), new Sort.Function([text], number).GetHashCode());
    }

    [Fact(DisplayName = "the compilation keeps each resolved annotation's sort, and no arity-wrong one")]
    public void TheCompilationKeepsEachResolvedAnnotationsSort()
    {
        var kept = Compilation.Of(new SourceText("type Car;\nvar x => list of number;\nvar y => Car;\n", "s.ron"));

        Assert.Empty(kept.Findings);
        Assert.Equal(new Sort[] { new Sort.List(new Sort.Scalar("number")), new Sort.Named("Car") },
                     kept.Types.Select(annotation => annotation.Type));

        // An arity-wrong annotation is kept with a null sort, its span still recorded.
        var arity = Compilation.Of(new SourceText("type a;\ntype b;\nvar m => optional (a = b);\n", "s.ron"));

        Assert.Empty(arity.Findings);
        Assert.Null(Assert.Single(arity.Types).Type);

        // A too-long annotation resolves to no tree, so it is kept as no sort at all.
        var chain = string.Concat(Enumerable.Repeat("optional ", Resolver.MaxLexemes + 1));
        var huge = Compilation.Of(new SourceText($"var z => {chain}number;\n", "s.ron"));

        Assert.Empty(huge.Types);
    }
}
