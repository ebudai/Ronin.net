// Copyright © 2026 Eric Budai

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ronin.Compiler;

/// <summary>
///     The reserved-word list, computed rather than remembered.
/// </summary>
///
/// <remarks>
///     <para>
///     A pattern's glue — the words after its first hole — may not appear in a
///     name. So the union of every in-scope pattern's glue IS this language's
///     reserved-word list, and nothing wrote it down. Three things want it and
///     prose satisfies none of them:
///     </para>
///     <list type="number">
///         <item>
///             a rule's message should name the pattern that reserved the word,
///             or it can only say "that word is taken", which tells the
///             programmer nothing about what to do
///         </item>
///         <item>
///             adding a pattern is a BREAKING change — names that were legal
///             stop being legal wherever it is in scope — and nothing notices
///         </item>
///         <item>
///             nobody can see what a pattern costs before committing to it
///         </item>
///     </list>
///     <para>
///     The seed registry in the handoff folder found «of» reserved by a single
///     badly-shaped pattern in about thirty seconds, after it had gone unnoticed
///     across every design document. That is the argument for generating it.
///     </para>
///     <para>
///     The lever is that a pattern whose words all precede its first hole
///     reserves nothing. Anchor-first is free; interleaving costs a word, for
///     every program, for as long as the pattern is in scope.
///     </para>
/// </remarks>
internal static class Glue
{
    /// <summary>Each reserved word, with the pattern that reserved it.</summary>
    public static IReadOnlyList<(string Word, string Pattern)> Reserved(IEnumerable<Pattern> patterns)
        => [.. patterns.SelectMany(pattern => pattern.Glue.Select(word => (Word: word, Pattern: pattern.ToString())))
                       .Distinct()
                       .OrderBy(entry => entry.Word, System.StringComparer.Ordinal)
                       .ThenBy(entry => entry.Pattern, System.StringComparer.Ordinal)];

    /// <summary>
    ///     The registry, as a file to check in and diff.
    /// </summary>
    ///
    /// <remarks>
    ///     Patterns that reserve nothing are listed too, and deliberately: the
    ///     shape to prefer is only visible beside the shape that costs something.
    /// </remarks>
    public static string Registry(IEnumerable<Pattern> patterns)
    {
        var declared = patterns.OrderBy(pattern => pattern.ToString(), System.StringComparer.Ordinal).ToArray();
        var reserved = Reserved(declared);

        StringBuilder registry = new();

        registry.AppendLine("# Reserved words, generated from the patterns in scope. Do not edit.");
        registry.AppendLine("#");
        registry.AppendLine("# A word here may not appear in a name wherever its pattern is visible.");
        registry.AppendLine("# Adding a line is a breaking change for every program that sees it.");
        registry.AppendLine();
        registry.AppendLine($"## RESERVED ({reserved.Count})");
        registry.AppendLine();

        foreach (var (word, pattern) in reserved) registry.AppendLine($"    {word,-12} {pattern}");

        var free = declared.Where(pattern => pattern.Glue.Any() is false).ToArray();

        registry.AppendLine();
        registry.AppendLine($"## RESERVES NOTHING ({free.Length}) — all words precede the first hole");
        registry.AppendLine();

        foreach (var pattern in free) registry.AppendLine($"    {pattern}");

        return registry.ToString();
    }
}
