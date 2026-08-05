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
///     A pattern's glue — the words after its first hole — may not appear inside a
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
///     The lever is that a word costs nothing unless a hole before it could grow
///     over it. Anchor-first is the easy way to get that — no hole precedes any
///     word — but not the only one: a DETERMINATE hole, pinned to one token or
///     required to be bracketed, cannot grow either, so the word after it is
///     free as well. Interleaving with an indeterminate hole costs a word, for
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
    ///     The heading says «determinate», not «all words precede the first hole»,
    ///     because the second is the common case and not the condition — «for each
    ///     (_) in (_)» has a word after a hole and still reserves nothing, since
    ///     the hole it follows is PINNED and so cannot grow over it.
    /// </remarks>
    /// <summary>
    ///     Every shape the compiler builds a name out of, and what causes it.
    /// </summary>
    ///
    /// <remarks>
    ///     Every one joins with a PROTECTED word, so no declaration anywhere can
    ///     break one — a name an author can type has to survive whatever anyone
    ///     else declares.
    ///     <para>
    ///     A chain's generated names are NOT here, and that is the point: nothing
    ///     it generates is ever typed, so they are reports rather than names —
    ///     «when a (waiting at 1)» cannot be written and so cannot collide. The
    ///     scheme that made them prose cost three protected words to make
    ///     internal identifiers look like a language they were never part of.
    ///     </para>
    /// </remarks>
    public static IReadOnlyList<(string Shape, string Cause)> Shapes { get; } =
        [.. Injection.All.Select(injection => (injection.Shape, injection.Cause))];

    public static string Registry(IEnumerable<Pattern> patterns)
    {
        var declared = patterns.OrderBy(pattern => pattern.ToString(), System.StringComparer.Ordinal).ToArray();
        var reserved = Reserved(declared);

        StringBuilder registry = new();

        registry.AppendLine("# Reserved words, generated from the patterns in scope. Do not edit.");
        registry.AppendLine("#");
        registry.AppendLine("# A word here may not appear INSIDE a name wherever its pattern is visible,");
        registry.AppendLine("# nor may a name be made only of words from this file. At an edge it is free:");
        registry.AppendLine("# «to uppercase» is a name, «time to live» is not.");
        registry.AppendLine("# Adding a line is a breaking change for every program that sees it.");
        registry.AppendLine("#");
        registry.AppendLine("# A pattern below is written as it is declared, and «guillemets» mark the one");
        registry.AppendLine("# thing that cannot be: a PINNED hole, which takes one word and has no");
        registry.AppendLine("# declaration syntax yet. Everything outside them is ordinary source.");
        registry.AppendLine();
        registry.AppendLine($"## RESERVED ({reserved.Count})");
        registry.AppendLine();

        foreach (var (word, pattern) in reserved) registry.AppendLine($"    {word,-12} {pattern}");

        // Reserved by the LANGUAGE and not by anything in scope, so it is not a
        // count that a program can change. A word operator is glue in everything
        // but name: a name spanning one is one lookup where the expression is
        // two, so it wins silently, and R5's remedy is the only one that reaches
        // it.
        registry.AppendLine();
        registry.AppendLine($"## ALWAYS RESERVED ({Rules.Infix.Count}) — a word operator, everywhere, in every scope");
        registry.AppendLine();

        foreach (var word in Rules.Infix)
            registry.AppendLine($"    {word,-12} reads as an operator between two values, so no name may have it inside");

        var free = declared.Where(pattern => pattern.Glue.Any() is false).ToArray();

        registry.AppendLine();
        registry.AppendLine($"## RESERVES NOTHING ({free.Length}) — every hole before a word is determinate");
        registry.AppendLine();

        foreach (var pattern in free) registry.AppendLine($"    {pattern}");

        // The other half of what a pattern costs, and the half a word count
        // cannot show. An anchor-only pattern reserves no word ANYWHERE — and it
        // does reserve its own word run as a name prefix, because a name
        // covering the whole call is one lookup where the call is at least two,
        // so it wins silently. A pattern with glue is not here: R5 already
        // refuses any name that could reach across it.
        var anchored = declared.Where(pattern => pattern.IsAnchorOnly
                                              && pattern.Segments.Any(segment => segment is null))
                               .ToArray();

        registry.AppendLine();
        registry.AppendLine($"## RESERVES A NAME PREFIX ({anchored.Length}) — no name may begin with these words");
        registry.AppendLine();

        foreach (var pattern in anchored)
        {
            registry.AppendLine($"    {string.Join(" ", pattern.Anchor),-12} from {pattern}");
        }

        // The dual list. Glue words may not be names; injection words may not be
        // glue — and a reader of this file wants both directions, because they
        // are the same trap seen from either end.
        //
        // «old» is here because it is protected, not because it is protected the
        // same way: it is refused in ANY segment and reported as a reserved word,
        // where «index» and «of» are refused as glue. That is a difference in
        // which rule fires, not in what a pattern author may write, and this file
        // answers the second question — so leaving it off made the list read as
        // complete when it was not.
        var protectedWords = Rules.Injected
                                  .Prepend((SymbolTable.Old,
                                            $"{Injection.Shadow.Shape}, and is refused in any segment, not only glue"))
                                  .ToArray();

        registry.AppendLine();
        registry.AppendLine($"## PROTECTED ({protectedWords.Length}) — no pattern may use these as glue");
        registry.AppendLine();

        foreach (var (word, injects) in protectedWords) registry.AppendLine($"    {word,-12} builds {injects}");

        // The other half of the same rule, and checked in for the same reason:
        // a collision between an injected name and a declared one should be
        // found by reading a diff rather than by hitting it. One scheme —
        // qualifier first, subject after, joined by a protected word — rather
        // than a spelling per injection.
        registry.AppendLine();
        registry.AppendLine($"## INJECTED ({Shapes.Count}) — names the compiler builds, and never asks anyone to rename");
        registry.AppendLine();

        foreach (var (shape, cause) in Shapes) registry.AppendLine($"    {shape,-28} {cause}");

        return registry.ToString();
    }
}
