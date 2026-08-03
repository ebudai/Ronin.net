// Copyright © 2026 Eric Budai

using System.Text.RegularExpressions;

namespace Unit;

/// <summary>
///     The spec's table of contents, as links that have to resolve.
/// </summary>
///
/// <remarks>
///     <para>
///     Written because the contents outlived a production it linked to. Indexing
///     stopped being a bracketed aggregate and became the «@» operator, the
///     section went, and the entry pointing at it stayed — along with a scatter
///     of anchors that had never matched a heading at all, including the section
///     numbers themselves: the body had two «4.4»s and everything after the
///     first drifted by one.
///     </para>
///     <para>
///     A prose sweep cannot keep finding that. Deleting a section is exactly the
///     moment nobody rereads the contents, so the check belongs where a deletion
///     already has to run.
///     </para>
/// </remarks>
[Trait("Documentation", null)]
public partial class SpecLinks
{
    private static readonly string Spec =
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "docs", "spec");

    /// <summary>A markdown link's text, file and optional anchor.</summary>
    [GeneratedRegex(@"\[(?<text>[^\]]*)\]\((?<file>[^)#]+)(?<anchor>#[^)]*)?\)")]
    private static partial Regex Link();

    /// <summary>
    ///     A heading as GitHub anchors it: lowercased, punctuation dropped,
    ///     spaces hyphenated.
    /// </summary>
    private static string Anchor(string heading)
        => string.Concat(heading.TrimStart('#', ' ')
                                .ToLowerInvariant()
                                .Select(character => character switch
                                {
                                    ' ' => '-',
                                    _ when char.IsLetterOrDigit(character) || character is '-' => character,
                                    _ => '\0',
                                })
                                .Where(character => character is not '\0'));

    private static HashSet<string> Anchors(string file)
        => [.. File.ReadLines(file)
                   .Where(line => line.StartsWith('#'))
                   .Select(Anchor)];

    [Fact(DisplayName = "every link in the spec's contents resolves to a heading that exists")]
    public void EveryLinkInTheSpecsContentsResolvesToAHeadingThatExists()
    {
        var contents = Path.Combine(Spec, "README.md");

        Assert.True(File.Exists(contents), $"{contents} is missing");

        var links = Link().Matches(File.ReadAllText(contents));

        // The contents is the spec's index and an empty match set would pass
        // every assertion below without reading anything.
        Assert.NotEmpty(links);

        foreach (Match link in links)
        {
            var target = Path.Combine(Spec, link.Groups["file"].Value);

            Assert.True(File.Exists(target), $"«{link.Value}» names a file that is not there");

            if (link.Groups["anchor"].Success is false) continue;

            Assert.Contains(link.Groups["anchor"].Value.TrimStart('#'), Anchors(target));
        }
    }

    [Fact(DisplayName = "and the contents names every section the grammar has")]
    public void AndTheContentsNamesEverySectionTheGrammarHas()
    {
        // The other direction, which is the one a DELETION passes and an
        // ADDITION does not: an entry pointing at nothing is caught above, and a
        // section nobody listed is caught here. Both halves, or the contents
        // drifts in whichever direction is not gated.
        var grammar = Path.Combine(Spec, "grammatical-structure.md");

        var sections = File.ReadLines(grammar)
                           .Where(line => Numbered().IsMatch(line))
                           .Select(Anchor);

        var listed = Link().Matches(File.ReadAllText(Path.Combine(Spec, "README.md")))
                           .Where(link => link.Groups["file"].Value is "grammatical-structure.md")
                           .Select(link => link.Groups["anchor"].Value.TrimStart('#'))
                           .ToHashSet();

        Assert.All(sections, section => Assert.Contains(section, listed));
    }

    /// <summary>A heading that carries a section number, which is one the contents owes an entry.</summary>
    [GeneratedRegex(@"^#+ \d")]
    private static partial Regex Numbered();
}
