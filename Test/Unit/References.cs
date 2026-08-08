// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using System.IO;

namespace Unit;

/// <summary>
///     The reference is what the language says about itself.
/// </summary>
///
/// <remarks>
///     Two gates, and they are deliberately only two. An entry with NO summary is
///     impossible rather than tested — it is a constructor parameter, so the
///     thought never occurs. What a type cannot see is whether a summary is
///     empty, and whether a cross-reference names anything; those are here.
///     <para>
///     Make the wrong state unrepresentable before making it detectable. A test
///     tells you an entry is missing its description; a required parameter means
///     nobody writes one without it.
///     </para>
/// </remarks>
[Trait(nameof(Manual), null)]
public class References
{
    private static readonly string Committed =
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "docs", "reference.md");

    [Fact(DisplayName = "every supplied thing says what it is")]
    public void EverySuppliedThingSaysWhatItIs()
    {
        // The type demands a summary; it cannot demand a useful one. A blank or a
        // placeholder satisfies the constructor and defeats the purpose, and
        // "TODO" in a generated reference is worse than an absent page because it
        // looks like the answer.
        Assert.All(SymbolTable.Supplies, supplied =>
        {
            Assert.False(string.IsNullOrWhiteSpace(supplied.Summary), $"{supplied.Name} has no summary");
            Assert.DoesNotContain("TODO", supplied.Summary, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith(".", supplied.Summary.Trim(), StringComparison.Ordinal);
        });
    }

    [Fact(DisplayName = "and every cross-reference names something that exists")]
    public void AndEveryCrossReferenceNamesSomethingThatExists()
    {
        // The pair this was built for exists BECAUSE each names the other, so a
        // cross-reference that can rot is one that will. Names rather than prose
        // is what makes this checkable at all — and it is why «stop» has no entry
        // here yet: it is a runtime operation with no source form, so the other
        // end does not exist and the reference to it is not written. A checked
        // reference with one end missing is the check working.
        var named = SymbolTable.Supplies.Select(supplied => supplied.Name).ToHashSet(StringComparer.Ordinal);

        Assert.All(SymbolTable.Supplies, supplied => Assert.All(supplied.SeeAlso,
            name => Assert.True(named.Contains(name), $"{supplied.Name} points at «{name}», which is not an entry")));
    }

    [Fact(DisplayName = "and the committed reference is what the table produces")]
    public void AndTheCommittedReferenceIsWhatTheTableProduces()
    {
        // The same discipline as the reserved-words registry, and the same
        // reason: a generated artefact nobody compares is a generated artefact
        // that silently stops matching. Normalised for line endings, because a
        // checkout's are its own business and not the language's.
        var reference = Manual.Of(SymbolTable.Supplies);

        Assert.True(File.Exists(Committed), $"{Committed} is missing — regenerate it");

        Assert.Equal(File.ReadAllText(Committed).ReplaceLineEndings("\n"), reference.ReplaceLineEndings("\n"));
    }
}
