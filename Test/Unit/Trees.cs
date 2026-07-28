// Copyright © 2026 Eric Budai

using Ronin.Compiler;

namespace Unit;

/// <summary>
///     The tree a resolved statement produces.
/// </summary>
///
/// <remarks>
///     The readings pinned in <c>Resolutions</c> are rendered from these nodes, so
///     those tests already constrain the shape — a wrong tree renders wrongly.
///     These walk it instead of reading it, which is what an interpreter does.
/// </remarks>
[Trait(nameof(Resolver), null)]
public class Trees
{
    [Fact(DisplayName = "a call carries its pattern and its arguments")]
    public void ACallCarriesItsPatternAndItsArguments()
    {
        SymbolTable symbols = new();
        symbols.WithNames("a", "b").WithPatterns("compute total for _");

        Resolver resolver = new(symbols);

        Assert.True(resolver.Resolve("compute total for a + b").TryTree(out var tree));

        var call = Assert.IsType<Node.Call>(tree);
        Assert.Equal("compute total for (_)", call.Pattern.ToString());

        var operation = Assert.IsType<Node.Operation>(Assert.Single(call.Arguments));
        Assert.Equal("+", operation.Symbol);
        Assert.Equal("a", Assert.IsType<Node.Name>(operation.Left).Words);
        Assert.Equal("b", Assert.IsType<Node.Name>(operation.Right).Words);
    }

    [Fact(DisplayName = "arguments arrive in hole order")]
    public void ArgumentsArriveInHoleOrder()
    {
        // «send _ to _» has two holes with a word between them, so the arguments
        // are not adjacent in the source and their order is the pattern's, not
        // the statement's.
        SymbolTable symbols = new();
        symbols.WithNames("alice", "hello").WithPatterns("send _", "send _ to _");

        Resolver resolver = new(symbols);

        Assert.True(resolver.Resolve("send hello to alice").TryTree(out var tree));

        var call = Assert.IsType<Node.Call>(tree);
        Assert.Equal(new[] { "hello", "alice" },
                     call.Arguments.Select(argument => ((Node.Name)argument).Words));
    }

    [Fact(DisplayName = "calls nest")]
    public void CallsNest()
    {
        SymbolTable symbols = new();
        symbols.WithNames("list").WithPatterns("print _", "sum of _");

        Resolver resolver = new(symbols);

        Assert.True(resolver.Resolve("print sum of sum of list").TryTree(out var tree));

        var print = Assert.IsType<Node.Call>(tree);
        var outer = Assert.IsType<Node.Call>(Assert.Single(print.Arguments));
        var inner = Assert.IsType<Node.Call>(Assert.Single(outer.Arguments));

        Assert.Equal("list", Assert.IsType<Node.Name>(Assert.Single(inner.Arguments)).Words);
    }

    [Fact(DisplayName = "literals and groups keep their contents")]
    public void LiteralsAndGroupsKeepTheirContents()
    {
        SymbolTable symbols = new();
        symbols.WithNames("a").WithPatterns("print _");

        Resolver resolver = new(symbols);

        Assert.True(resolver.Resolve("print 42").TryTree(out var literal));
        Assert.Equal("42", Assert.IsType<Node.Literal>(Assert.Single(((Node.Call)literal).Arguments)).Text);

        Assert.True(resolver.Resolve("print (a)").TryTree(out var bracketed));
        var group = Assert.IsType<Node.Group>(Assert.Single(((Node.Call)bracketed).Arguments));
        Assert.Equal("a", Assert.IsType<Node.Name>(Assert.Single(group.Parts)).Words);
    }

    [Fact(DisplayName = "no tree without a single meaning")]
    public void NoTreeWithoutASingleMeaning()
    {
        SymbolTable symbols = new();
        symbols.WithNames("list", "of list").WithPatterns("sum of _", "sum _");

        Resolver resolver = new(symbols);

        Assert.False(resolver.Resolve("bogus").TryTree(out _));

        // a tie has two trees and no grounds to pick one, so it hands out neither
        Assert.False(resolver.Resolve("sum of list").TryTree(out _));

        Assert.True(resolver.Resolve("sum of (list)").TryTree(out _));
    }

    [Fact(DisplayName = "a node renders as its reading")]
    public void ANodeRendersAsItsReading()
    {
        // The rendering is derived from the tree rather than accumulated while
        // parsing, and it has to stay identical: every expectation transcribed
        // from the Python reference is written as one of these strings.
        SymbolTable symbols = new();
        symbols.WithNames("a", "b").WithPatterns("compute total for _");

        Resolver resolver = new(symbols);
        var resolution = resolver.Resolve("compute total for (a) + b");

        Assert.True(resolution.TryTree(out var tree));
        Assert.Equal("compute total for (⟨«a»⟩ + «b»)", tree.ToString());
        Assert.Equal(resolution.Reading, tree.ToString());
    }
}
