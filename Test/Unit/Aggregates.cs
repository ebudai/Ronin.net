// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Runtime;

namespace Unit;

/// <summary>
///     «=» is an association separator, never an expression operator.
/// </summary>
///
/// <remarks>
///     The invariant <see cref="Collection"/> parses on. A «[…]» is one production
///     and one decision only because «=» inside brackets can mean exactly one
///     thing: were it an operator too, «is this an association?» could not be
///     answered without parsing the key both ways, and the exponential the single
///     parse removed would return through a door nobody is watching. The constraint
///     lived in a comment, which is the one place with no consumer — and the ladder
///     work is about to make the operator table user-extensible, which is the door.
/// </remarks>
[Trait(nameof(Collection), null)]
public class Aggregates
{
    [Fact(DisplayName = "= is an association separator, never an operator")]
    public void EqualsIsAnAssociationSeparatorNeverAnOperator()
        => Assert.False(Builtin.Operators.ContainsKey(Assign.symbol.ToString()),
            "«=» inside brackets is only ever an association separator, never an expression operator. "
          + "If that ever stops being true, «Collection»'s single parse becomes a guess and the exponential returns.");

    private static Resolution Resolve(string source, params string[] names)
    {
        SymbolTable symbols = new();

        symbols.WithNames(names);

        return new Resolver(symbols).Resolve(Lexemes.Lex(source));
    }

    [Fact(DisplayName = "a lookup literal resolves, as a lookup and not as a list")]
    public void ALookupLiteralResolvesAsALookupAndNotAsAList()
    {
        var resolution = Resolve("[ a = 1, b = 2 ]", "a", "b");

        Assert.Equal(ResolutionKind.Resolved, resolution.Kind);
        Assert.True(resolution.TryTree(out var tree));

        var lookup = Assert.IsType<Node.Group>(tree);

        Assert.Equal(Node.Grouping.Lookup, lookup.Kind);
        Assert.Equal(2, lookup.Parts.Count);
        Assert.Equal("a", Assert.IsType<Node.Name>(lookup.Parts[0].Key).Words);
        Assert.Equal("1", Assert.IsType<Node.Literal>(lookup.Parts[0].Value).Text);
        Assert.Equal("[«a» = 1, «b» = 2]", lookup.ToString());

        // A LIST is the other kind, and keeps its null keys.
        var list = Assert.IsType<Node.Group>(Resolve("[ a, b ]", "a", "b").TryTree(out var read) ? read : null);

        Assert.Equal(Node.Grouping.List, list.Kind);
        Assert.Null(list.Parts[0].Key);
    }

    [Fact(DisplayName = "two lookups differing only in their keys are two derivations")]
    public void TwoLookupsDifferingOnlyInTheirKeysAreTwoDerivations()
    {
        Assert.True(Resolve("[ a = 1 ]", "a", "b").TryTree(out var first));
        Assert.True(Resolve("[ b = 1 ]", "a", "b").TryTree(out var second));

        // The key is part of the shape. Were it kept beside the parts instead of
        // in them, these would compare the same and one meaning would swallow
        // the other.
        Assert.False(Node.Same.Equals(first, second));

        // And a lookup is not the list of its values.
        Assert.True(Resolve("[ 1 ]").TryTree(out var alone));
        Assert.True(Resolve("[ a = 1 ]", "a").TryTree(out var keyed));
        Assert.False(Node.Same.Equals(alone, keyed));
    }

    [Theory(DisplayName = "an entry the resolver cannot read as one association is not a derivation")]
    [InlineData("[ a = 1, 2 ]")]           // part value, part association
    [InlineData("[ 1, a = 2 ]")]           // the same, the other way round
    [InlineData("[ a = 1 = 2 ]")]          // two «=» in one entry
    [InlineData("[ = 1 ]")]                // no key
    [InlineData("( a = 1 )")]              // «=» is a collection's, not a grouping's
    public void AnEntryTheResolverCannotReadAsOneAssociationIsNotADerivation(string source)
        => Assert.Equal(ResolutionKind.NoParse, Resolve(source, "a", "b").Kind);

    [Fact(DisplayName = "a lookup evaluates to the runtime lookup value")]
    public void ALookupEvaluatesToTheRuntimeLookupValue()
    {
        Assert.True(Resolve("[ a = 1, 2 = 3 ]", "a").TryTree(out var tree));

        Graph graph = new();
        graph.Var("a", 7d);

        var value = Assert.IsType<Lookup>(new Evaluator(new Ronin.Runtime.Scope()).Evaluate(graph, tree, insideLet: false));

        // The keys arrive with the value, which is what they have never done.
        Assert.Equal(2, value.Count);
        Assert.Equal(7d, value[0].Key);
        Assert.Equal(1d, value[0].Value);
        Assert.Equal(2d, value[1].Key);
        Assert.Equal(3d, value[1].Value);

        // And a list still evaluates to a list, at one element as at many.
        Assert.True(Resolve("[ 1, 2 ]").TryTree(out var listed));
        Assert.IsType<List>(new Evaluator(new Ronin.Runtime.Scope()).Evaluate(graph, listed, insideLet: false));

        // A duplicate key by VALUE is refused at the boundary, where the parser's
        // spelled check cannot reach: «a» is 7 and so is the literal.
        Assert.True(Resolve("[ a = 1, 7 = 2 ]", "a").TryTree(out var twice));
        Assert.Contains("same key",
            Assert.IsType<Error>(new Evaluator(new Ronin.Runtime.Scope()).Evaluate(graph, twice, insideLet: false)).Message);
    }
}
