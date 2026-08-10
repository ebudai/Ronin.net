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
///     This asks the search: for each reading the finding displays, it must have
///     produced a repair, and that repair must apply to a file that compiles and
///     reads the way it was named.
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

    [Fact(DisplayName = "every displayed reading of a generated ambiguity has a repair that selects it")]
    public void EveryDisplayedReadingOfAGeneratedAmbiguityHasARepairThatSelectsIt()
    {
        Random random = new(Seed: 20260810);
        var ambiguities = 0;

        for (var trial = 0; trial < 1000; ++trial)
        {
            var source = Declarations + Result + Expression(random) + ";\n";

            foreach (var finding in Compilation.Of(new SourceText(source, "gen.ron")).Findings.OfType<Ambiguous>())
            {
                ++ambiguities;

                // ONE repair per displayed reading. Fewer means the search dropped
                // a meaning it showed — the failure of audits ten through eighteen,
                // each a reading with no way to select it.
                Assert.Equal(finding.Readings.Count, finding.Repairs.Count);

                var edited = finding.Repairs.Select(repair => Applied(source, repair)).ToArray();

                // A different edit for each, so no two readings share one bracketing.
                Assert.Equal(finding.Repairs.Count, edited.Distinct(StringComparer.Ordinal).Count());

                foreach (var (repair, variant) in finding.Repairs.Zip(edited))
                {
                    var recompiled = Compilation.Of(new SourceText(variant, "gen.ron"));

                    // It applies to a file that compiles — no ambiguity left, no
                    // other complaint — and reads the way it was named. Group marks
                    // are stripped, name marks kept, so «a to b» is not «a» to «b».
                    Assert.Empty(recompiled.Findings);
                    Assert.Equal(Grouped(repair.Reading), Grouped(Selected(recompiled)));
                }
            }
        }

        // It met the ambiguities to prove the property over, exactly — a property
        // proved over none passes forever, and the count catches a generator that
        // quietly stops producing them.
        Assert.Equal(372, ambiguities);
    }

    /// <summary>The reading a repaired file resolves its result statement to.</summary>
    private static string Selected(Compilation recompiled)
        => recompiled.Readings
                     .Where(reading => reading.Resolution.Kind is ResolutionKind.Resolved
                                    && reading.Span.Offset >= Declarations.Length)
                     .OrderByDescending(reading => reading.Span.Length)
                     .First()
                     .Resolution.Reading;

    /// <summary>A reading with the grouping marks removed but the name marks kept.</summary>
    private static string Grouped(string reading)
        => reading.Replace("⟨", string.Empty, StringComparison.Ordinal)
                  .Replace("⟩", string.Empty, StringComparison.Ordinal);

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
