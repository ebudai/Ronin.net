// Copyright © 2026 Eric Budai

using Ronin.Compiler;

namespace Unit;

/// <summary>
///     Every displayed reading of a generated ambiguity has a repair that selects it.
/// </summary>
///
/// <remarks>
///     <para>
///     The property the last several audits found broken, one round at a time: a
///     statement resolved as ambiguous, its readings shown, and one of those
///     readings had no repair — a meaning the editor names and cannot select.
///     Nothing in the suite asked, because the maintained repair cases are
///     fixtures, each the exact shape a reader thought to write down. The bugs
///     were in the shapes nobody thought of.
///     </para>
///     <para>
///     GENERATED and run through production «Compilation», not a hand-built table.
///     The older completeness test searches for a bracket itself and asks the
///     resolver which readings exist — so it proves a bracket EXISTS, not that the
///     repair search FINDS it, and the two came apart in every audit from ten on.
///     This asks the search: for each meaning the finding displays, it must have
///     produced a repair, and that repair must apply to a file that compiles and
///     resolve to that meaning's TREE — by shape, with «Node.Same», not by a
///     rendering that two nested calls can share.
///     </para>
///     <para>
///     SEEDED rather than timed, so a failure is a case anyone can reproduce, and
///     asserted to have met a floor of ambiguities — a completeness property
///     proved over no ambiguous statement passes forever, which is the mistake
///     that sent an earlier version of this idea down the wrong path.
///     </para>
/// </remarks>
[Trait(nameof(Compilation), null)]
public class RepairCoverage
{
    /// <summary>
    ///     Patterns and names whose calls nest and overlap — the family every
    ///     audited reproduction was drawn from.
    /// </summary>
    ///
    /// <remarks>
    ///     Two one-and-two-argument shapes so a call's second argument can be a
    ///     name or the start of a fixed word; a bare wrapper so calls stack; and
    ///     the interior-glue names «a to b» / «b to a» a call can read as one
    ///     argument or split across its own «to».
    /// </remarks>
    private const string Declarations =
        "function wrap (x => Number) { return x; }\n"
      + "function print (x => Number) { return x; }\n"
      + "function print (x => Number) to (y => Number) { return x; }\n"
      + "function send (x => Number) { return x; }\n"
      + "function send (x => Number) to (y => Number) { return x; }\n"
      + "var a => Number;\nvar b => Number;\nvar a to b => Number;\nvar b to a => Number;\n";

    private const string Result = "var result = ";

    private static readonly string[] callers = ["wrap", "print", "send"];
    private static readonly string[] names = ["a", "b", "a to b", "b to a"];
    private static readonly string[] ends = ["a", "b"];

    /// <summary>
    ///     A random expression over the declared vocabulary: some callers, a name,
    ///     some «to» tails — the composition the reproductions all took.
    /// </summary>
    private static string Expression(Random random)
    {
        List<string> words = [];

        for (var caller = random.Next(0, 5); caller > 0; --caller) words.Add(callers[random.Next(callers.Length)]);

        words.Add(names[random.Next(names.Length)]);

        for (var tail = random.Next(0, 4); tail > 0; --tail) { words.Add("to"); words.Add(ends[random.Next(ends.Length)]); }

        return string.Join(' ', words);
    }

    [Fact(DisplayName = "every displayed reading of a generated ambiguity has a repair that structurally selects it")]
    public void EveryDisplayedReadingOfAGeneratedAmbiguityHasARepairThatStructurallySelectsIt()
    {
        Random random = new(Seed: 20260810);
        var ambiguities = 0;

        for (var trial = 0; trial < 1000; ++trial)
        {
            var source = Declarations + Result + Expression(random) + ";\n";

            foreach (var reading in Compilation.Of(new SourceText(source, "gen.ron")).Readings
                                               .Where(reading => reading.Resolution.Kind is ResolutionKind.Ambiguous))
            {
                ++ambiguities;

                // The competing readings as TREES, and the repairs in the order the
                // search produced them — one per tree. Fewer means the search
                // dropped a meaning it showed, the failure of audits ten through
                // eighteen.
                var alternatives = reading.Resolution.Alternatives;
                var repairs = reading.Repairs;

                Assert.Equal(alternatives.Count, repairs.Count);

                List<Node> selected = [];

                foreach (var (alternative, repair) in alternatives.Zip(repairs))
                {
                    var recompiled = Compilation.Of(new SourceText(Applied(source, repair), "gen.ron"));

                    // It applies to a file that compiles — no ambiguity left, no
                    // other complaint.
                    Assert.Empty(recompiled.Findings);

                    // And selects the TREE it was built for, not merely one that
                    // renders the same. Two nested calls print alike, so a
                    // rendering here would pass for a repair that reached the other
                    // one of the pair. The repair's brackets come off both sides
                    // and «Node.Same» asks whether the shapes are then the same.
                    var tree = Stripped(Selected(recompiled));

                    Assert.Equal(Stripped(alternative), tree, Node.Same);

                    selected.Add(tree);
                }

                // Distinct SHAPES, so the repairs cover the displayed meanings one
                // to one — not two edits selecting one meaning while another, read
                // alike, goes unrepaired.
                Assert.Equal(repairs.Count, selected.Distinct(Node.Same).Count());
            }
        }

        // It met the ambiguities to prove the property over, exactly — a property
        // proved over none passes forever, and the count catches a generator that
        // quietly stops producing them.
        Assert.Equal(372, ambiguities);
    }

    /// <summary>The tree a repaired file resolves its result statement to.</summary>
    private static Node Selected(Compilation recompiled)
    {
        var resolution = recompiled.Readings
                                   .Where(reading => reading.Resolution.Kind is ResolutionKind.Resolved
                                                  && reading.Span.Offset >= Declarations.Length)
                                   .OrderByDescending(reading => reading.Span.Length)
                                   .First()
                                   .Resolution;

        resolution.TryTree(out var tree);

        return tree;
    }

    /// <summary>
    ///     A tree with the brackets a repair added stripped away, the same way the
    ///     repair search compares a candidate to its target.
    /// </summary>
    ///
    /// <remarks>
    ///     A bracket around one value is a no-op grouping, so single non-collection
    ///     groups come off, and a call is recursed into because that is where a
    ///     repair puts them. The generated grammar has no operators, so an
    ///     operation never appears here.
    /// </remarks>
    private static Node Stripped(Node tree)
    {
        var bare = Bare(tree);

        return bare is Node.Call call ? new Node.Call(call.Pattern, [.. call.Arguments.Select(Stripped)]) : bare;
    }

    private static Node Bare(Node tree)
        => tree is Node.Group { Kind: Node.Grouping.Group, Parts.Count: 1 } group ? Bare(group.Parts[0].Value) : tree;

    /// <summary>The source with one repair's brackets typed in.</summary>
    private static string Applied(string source, Repair repair)
    {
        var edited = source;

        foreach (var insertion in repair.Insertions.OrderByDescending(insertion => insertion.At))
        {
            edited = edited[..insertion.At] + insertion.Text + edited[insertion.At..];
        }

        return edited;
    }
}
