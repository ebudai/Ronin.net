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
    public static string Registry(IEnumerable<Pattern> patterns)
    {
        // SOUND ones only, and by the same predicate the diagnostics use. This
        // built its tables from everything it was handed, so a pattern refused
        // for an operator word still had its refinement printed here — under a
        // header saying these are the patterns in scope and that adding a line
        // is a breaking change. It cannot enter the language, so it reserves
        // nothing, and a safety report that says otherwise is worse than none.
        var declared = patterns.Where(Rules.Sound)
                               .OrderBy(pattern => pattern.ToString(), System.StringComparer.Ordinal)
                               .ToArray();
        var reserved = Reserved(declared);

        StringBuilder registry = new();

        registry.AppendLine("# Reserved words, generated from the patterns in scope. Do not edit.");
        registry.AppendLine("#");
        registry.AppendLine("# Most of this is ADVICE now. A name may contain any of these words: a");
        registry.AppendLine("# statement with two readings is an error that offers the bracketings, so");
        registry.AppendLine("# nothing is refused at the declaration for what it might do to a reading.");
        registry.AppendLine("# Using a word here still costs a bracket wherever the two meanings meet.");
        registry.AppendLine("#");
        registry.AppendLine("# Three sections are still rules, and say so: no name may BEGIN with the words");
        registry.AppendLine("# of an anchor-only pattern; no complete name may also have a glued or pinned");
        registry.AppendLine("# pattern's shape; and no pattern may use a word operator or injection word.");
        registry.AppendLine("# Those competing name readings are not reachable by bracketing.");
        registry.AppendLine("#");
        registry.AppendLine("# A pattern below is written as it is declared, and «guillemets» mark the one");
        registry.AppendLine("# thing that cannot be: a PINNED hole, which takes one word and has no");
        registry.AppendLine("# declaration syntax yet. Everything outside them is ordinary source.");
        registry.AppendLine();
        registry.AppendLine($"## COSTS A BRACKET INSIDE A NAME ({reserved.Count}) — advice");
        registry.AppendLine();

        foreach (var (word, pattern) in reserved) registry.AppendLine($"    {word,-12} {pattern}");

        // Reserved by the LANGUAGE and not by anything in scope, so it is not a
        // count that a program can change. A word operator is glue in everything
        // but name: a span containing one reads as the operation whatever else
        // it also reads as, and no bracketing tells the two apart.
        registry.AppendLine();
        registry.AppendLine($"## NO PATTERN MAY USE ({Rules.Infix.Count}) — a word operator, everywhere, in every scope");
        registry.AppendLine();

        foreach (var word in Rules.Infix)
            registry.AppendLine($"    {word,-12} reads as an operator between two values, and no bracket tells the two apart");

        var free = declared.Where(pattern => pattern.Glue.Any() is false).ToArray();

        registry.AppendLine();
        registry.AppendLine($"## COSTS NOTHING ({free.Length}) — every hole before a word is determinate");
        registry.AppendLine();

        foreach (var pattern in free) registry.AppendLine($"    {pattern}");

        // The other half of what a pattern costs, and the half a word count
        // cannot show. An anchor-only pattern reserves no word ANYWHERE — and it
        // does reserve its own word run as a name prefix, because a name
        // covering the whole call reads as that call too, and no bracketing
        // inside the name selects the name. A pattern with glue is not a
        // blanket prefix reservation; the rule asks whether a complete name
        // actually conforms to that pattern instead.
        var anchored = declared.Where(pattern => pattern.IsAnchorOnly
                                              && pattern.Segments.Any(segment => segment is null))
                               .ToArray();

        registry.AppendLine();
        registry.AppendLine($"## NO NAME MAY BEGIN WITH ({anchored.Length}) — a rule, not advice");
        registry.AppendLine();

        foreach (var pattern in anchored)
        {
            registry.AppendLine($"    {string.Join(" ", pattern.Anchor),-12} from {pattern}");
        }

        var shaped = declared.Where(pattern => pattern.IsAnchorOnly is false
                                            && pattern.Segments.Any(segment => segment is null))
                             .ToArray();

        registry.AppendLine();
        registry.AppendLine($"## NO COMPLETE NAME MAY HAVE THE SHAPE ({shaped.Length}) — a rule, not advice");
        registry.AppendLine();

        foreach (var pattern in shaped) registry.AppendLine($"    {pattern}");

        // The dual list. Glue words may not be names; injection words may not be
        // glue — and a reader of this file wants both directions, because they
        // are the same trap seen from either end.
        //
        // «old» is no longer an injection word. It appears above as the anchor
        // of «old (_)», where its actual cost is visible: a name prefix rather
        // than a blanket ban on pattern segments.
        var protectedWords = Rules.Injected.ToArray();

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
