// Copyright © 2026 Eric Budai

using System.Collections.Generic;
using System.Linq;

namespace Ronin.Compiler;

/// <summary>One bracket, and where it goes.</summary>
///
/// <param name="At">An offset in the source the statement was lexed from.</param>
internal readonly record struct Insertion(int At, string Text);

/// <summary>
///     A reading, and the edit that selects it.
/// </summary>
///
/// <remarks>
///     <para>
///     A message cannot be clicked. The design asked for the bracketings to be
///     IN the error and selectable, which means edits with positions rather than
///     a sentence describing where a bracket would go — an editor applies the
///     first and can only print the second.
///     </para>
///     <para>
///     <see cref="Rank"/> is the order to offer them in, cheapest first, and is
///     the whole of what cost does now. It may order the suggestions and it may
///     never choose among them: the moment it chooses, every silent capture this
///     design removed comes back looking like a feature.
///     </para>
/// </remarks>
internal sealed class Repair
{
    public Repair(string reading, int rank, IReadOnlyList<Insertion> insertions)
    {
        Reading = reading;
        Rank = rank;
        Insertions = Owned.Copy(insertions);
    }

    public string Reading { get; }

    public int Rank { get; }

    /// <summary>The brackets to type, owned where the repair is made.</summary>
    ///
    /// <remarks>
    ///     Positional would have made the caller's list a public promise of its
    ///     own beside the owned one, which is the same value handed out twice
    ///     with only one of them safe.
    /// </remarks>
    public IReadOnlyList<Insertion> Insertions { get; }
}

/// <summary>
///     The bracketings that select each reading of an ambiguous statement.
/// </summary>
///
/// <remarks>
///     <para>
///     ONE PAIR, always, at these lengths — searched rather than assumed. The
///     repair-completeness property found that a single bracket pair reaches
///     every reading of every ambiguous statement in a 19,525-statement space,
///     twice over, where the design allowed for two. So this looks for one pair
///     and reports honestly when it finds none, rather than pretending a
///     suggestion exists.
///     </para>
///     <para>
///     MINIMAL because a suggestion that brackets everything is correct and
///     useless. The narrowest span that selects the reading is the one a person
///     would have written, and it is found by trying narrow spans first.
///     </para>
///     <para>
///     By RESOLVING the candidate rather than by reasoning about the tree. The
///     claim a repair makes is "type this and the ambiguity is gone", so the
///     only honest way to produce one is to type it and look — which is what the
///     property test does, and it is why the property is a property rather than
///     a hope.
///     </para>
/// </remarks>
internal static class Repairs
{
    public static IReadOnlyList<Repair> For(Resolver resolver, IReadOnlyList<Lexeme> lexemes, Resolution ambiguity)
    {
        List<Repair> found = [];

        foreach (var alternative in ambiguity.Alternatives)
        {
            // NOT PUBLISHED when there is none. A repair with no insertions
            // looks selectable in an editor and does nothing when selected,
            // which is worse than an error that offers nothing — the second
            // says where you are and the first lies about it.
            if (Selecting(resolver, lexemes, alternative) is not IReadOnlyList<Insertion> insertions) continue;

            found.Add(new Repair(alternative.ToString(), found.Count, insertions));
        }

        // Owned on the way out, like every other value this compiler hands
        // over: what an editor is about to apply should not change under it
        // because the thing that built it kept a reference.
        return Owned.Copy(found);
    }

    /// <summary>The narrowest bracket pair that leaves only this reading.</summary>
    ///
    /// <remarks>
    ///     A reading is a fragment of the whole statement's rendering: the
    ///     witness carries the readings of the span where the tie IS, which a
    ///     parent cannot see. So selecting one means the bracketed statement
    ///     resolves uniquely and reads that way over that span, not that its
    ///     whole rendering equals it.
    /// </remarks>
    private static IReadOnlyList<Insertion> Selecting(Resolver resolver,
                                                      IReadOnlyList<Lexeme> lexemes,
                                                      Node target)
    {
        for (var width = 1; width <= lexemes.Count; ++width)
        {
            for (var from = 0; from + width <= lexemes.Count; ++from)
            {
                var resolution = resolver.Resolve(Bracketed(lexemes, from, from + width));

                if (resolution.TryTree(out var tree) is false) continue;
                if (Same(tree, target) is false) continue;

                return Owned.Copy<Insertion>(
                [
                    new Insertion(lexemes[from].Offset, "("),
                    new Insertion(lexemes[from + width - 1].Offset + lexemes[from + width - 1].Length, ")"),
                ]);
            }
        }

        return null;
    }

    /// <summary>
    ///     Whether two trees are the same reading, ignoring the brackets a
    ///     repair added.
    /// </summary>
    ///
    /// <remarks>
    ///     A repair works by GROUPING, so what it produces is the target with a
    ///     bracket somewhere in it — never the target itself. Unwrapping single
    ///     bracketed parts is what makes "did this select the reading" a question
    ///     about the reading.
    ///     <para>
    ///     This compared RENDERINGS, and stripped the bracket marks out of a
    ///     string to do it. That recreated one layer later the very defect the
    ///     cell had been taught to avoid: two calls spanning the same words
    ///     render alike, so both searches found the same bracket and one meaning
    ///     was offered twice while the other was unreachable.
    ///     </para>
    /// </remarks>
    private static bool Same(Node tree, Node target)
    {
        var here = Ungrouped(tree);
        var there = Ungrouped(target);

        if (Node.Same.Equals(here, there)) return true;

        return here is Node.Call call
            && there is Node.Call other
            && call.Pattern.Equals(other.Pattern)
            && call.Arguments.Count == other.Arguments.Count
            && call.Arguments.Zip(other.Arguments).All(pair => Same(pair.First, pair.Second));
    }

    /// <summary>A tree without the brackets around it.</summary>
    private static Node Ungrouped(Node tree)
        => tree is Node.Group { Collection: false, Parts.Count: 1 } group ? Ungrouped(group.Parts[0]) : tree;

    private static List<Lexeme> Bracketed(IReadOnlyList<Lexeme> lexemes, int from, int to)
    {
        List<Lexeme> bracketed = [.. lexemes.Take(from)];

        bracketed.Add(new Lexeme(LexemeKind.Open, "("));
        bracketed.AddRange(lexemes.Skip(from).Take(to - from));
        bracketed.Add(new Lexeme(LexemeKind.Close, ")"));
        bracketed.AddRange(lexemes.Skip(to));

        return bracketed;
    }

}
