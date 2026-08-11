// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Runtime;

namespace Unit;

/// <summary>
///     What makes two derivations one derivation.
/// </summary>
///
/// <remarks>
///     <para>
///     BY HAND, which the rest of this suite avoids and which this needs. A
///     derivation's identity is asked by a dictionary, so what the resolver can
///     reach is only ever the pairs a real table happens to produce — and those
///     are either the same object or two shapes that already hash apart. The
///     answers that decide correctness are the ones a hash collision would ask
///     for, and no source arranges one.
///     </para>
///     <para>
///     Which is not a licence to invent: every node here is a shape the resolver
///     builds, and the question asked of it is the one <c>Cell</c> asks. What is
///     synthetic is the pairing, not the parts.
///     </para>
///     <para>
///     The defect this guards was a comment that had become a claim — two
///     derivations that read the same way ARE the same reading — which nested
///     calls made false. A rendering is a sentence about a tree and two trees can
///     share one.
///     </para>
/// </remarks>
[Trait(nameof(Resolver), null)]
public class Derivations
{
    private static readonly Operator Plus = Builtin.Operators["+"];

    private static Node Name(string words) => new Node.Name(words);

    private static Node Grouped(Node part, Node.Grouping kind = Node.Grouping.Group)
        => new Node.Group([new Node.Entry(null, part)], kind);

    private static Node Call(string pattern, params Node[] arguments)
        => new Node.Call(Pattern.Parse(pattern), arguments);

    /// <summary>Alike both ways, because an equality that is not symmetric is not one.</summary>
    private static bool Alike(Node node, Node other)
    {
        var alike = Node.Same.Equals(node, other);

        Assert.Equal(alike, Node.Same.Equals(other, node));

        // A hash that disagrees with equality is worse than either being wrong:
        // the dictionary looks in one bucket and the answer sits in another, so
        // two identical derivations are counted twice and the statement is
        // called ambiguous with the same reading listed either side.
        if (alike) Assert.Equal(Node.Same.GetHashCode(node), Node.Same.GetHashCode(other));

        return alike;
    }

    [Fact(DisplayName = "the same shape twice is one derivation")]
    public void TheSameShapeTwiceIsOneDerivation()
    {
        Assert.True(Alike(new Node.Literal("1"), new Node.Literal("1")));
        Assert.True(Alike(Name("a"), Name("a")));
        Assert.True(Alike(new Node.Binding("a"), new Node.Binding("a")));
        Assert.True(Alike(Grouped(Name("a")), Grouped(Name("a"))));
        Assert.True(Alike(new Node.Operation(Name("a"), "+", Plus, Name("b")),
                          new Node.Operation(Name("a"), "+", Plus, Name("b"))));
        Assert.True(Alike(new Node.Previous("x", Name("x")), new Node.Previous("x", Name("x"))));

        // The pattern by VALUE, so two identical declarations of one shape stay
        // one derivation — that collapsing belongs to overloading and is not
        // this rule's to undo.
        Assert.True(Alike(Call("send _", Name("a")), Call("send _", Name("a"))));
    }

    [Fact(DisplayName = "and a different shape is not, however alike it reads")]
    public void AndADifferentShapeIsNotHoweverAlikeItReads()
    {
        Assert.False(Alike(new Node.Literal("1"), new Node.Literal("2")));
        Assert.False(Alike(Name("a"), Name("b")));
        Assert.False(Alike(new Node.Binding("a"), new Node.Binding("b")));
        Assert.False(Alike(Grouped(Name("a")), Grouped(Name("b"))));
        Assert.False(Alike(new Node.Previous("x", Name("x")), new Node.Previous("y", Name("y"))));

        // A COLLECTION and a grouping, which the resolver once could not tell
        // apart either: «(x)» is one value in brackets and «[x]» is a list of
        // one, and they differ in nothing else.
        Assert.False(Alike(Grouped(Name("a")), Grouped(Name("a"), Node.Grouping.List)));

        Assert.False(Alike(new Node.Operation(Name("a"), "+", Plus, Name("b")),
                           new Node.Operation(Name("a"), "+", Plus, Name("c"))));

        Assert.False(Alike(new Node.Operation(Name("a"), "+", Plus, Name("b")),
                           new Node.Operation(Name("a"), "-", Builtin.Operators["-"], Name("b"))));

        Assert.False(Alike(Call("send _", Name("a")), Call("print _", Name("a"))));
        Assert.False(Alike(Call("send _", Name("a")), Call("send _", Name("b"))));
    }

    [Fact(DisplayName = "and a name is not the binding that introduces it")]
    public void AndANameIsNotTheBindingThatIntroducesIt()
        // They render identically — «a» either way — because that is how a
        // reader wrote it. The resolver worked out which one this occurrence is,
        // and an identity keyed on the rendering throws that away again: a loop
        // variable and a reference to something already in scope would be one
        // derivation, which is the confusion the binding node exists to end.
        => Assert.False(Alike(Name("a"), new Node.Binding("a")));

    [Theory(DisplayName = "and no kind is mistaken for another")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void AndNoKindIsMistakenForAnother(int which)
    {
        // Every kind against every other, which a dictionary asks only when two
        // hashes collide. That is rare enough never to have happened and common
        // enough to be certain of: a kind that answered for another would merge
        // two meanings, and the merge is silent by construction.
        Node[] kinds =
        [
            new Node.Literal("a"),
            Name("a"),
            new Node.Binding("a"),
            Grouped(Name("a")),
            new Node.Operation(Name("a"), "+", Plus, Name("a")),
            new Node.Previous("a", Name("a")),
            Call("send _", Name("a")),
        ];

        foreach (var other in kinds)
        {
            if (ReferenceEquals(kinds[which], other)) continue;

            Assert.False(Alike(kinds[which], other));
        }
    }
}
