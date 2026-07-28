// Copyright © 2026 Eric Budai

using System.Collections.Generic;
using System.Linq;

namespace Ronin.Compiler;

/// <summary>
///     A name as declared, and where.
/// </summary>
///
/// <param name="InjectedBy">
///     The declaration that generated this name, when nobody wrote it. An
///     injected symbol has no text of its own, so it carries the span of its
///     origin — which is also the only thing a diagnostic can ask anyone to
///     change, since «old smoothed» is not the programmer's to rename.
/// </param>
internal readonly record struct Declared(string Name, Span Span, string InjectedBy = null);

/// <summary>
///     The scope-wide rules, checked over what was declared rather than over the
///     table the resolver probes.
/// </summary>
///
/// <remarks>
///     <para>
///     <see cref="SymbolTable"/> stays a lookup structure: the resolver asks it
///     whether a span of words is a name, once per position per span, and a set
///     is what that wants. Provenance would be dead weight on that path and is
///     what this takes as input instead.
///     </para>
///     <para>
///     Both rules came out of exhaustive search rather than judgement, and both
///     apply to the merged scope, so an inner declaration can invalidate an outer
///     name.
///     </para>
/// </remarks>
internal static class Rules
{
    public static IEnumerable<Finding> Validate(IReadOnlyCollection<Declared> names,
                                                IReadOnlyCollection<(Pattern Pattern, Span Span)> patterns)
    {
        foreach (var finding in Anchors(patterns)) yield return finding;
        foreach (var finding in Reserved(patterns)) yield return finding;
        foreach (var finding in Glue(names, patterns)) yield return finding;
    }

    /// <summary>
    ///     R6. Anchor runs must be prefix free, or «b (_)» and «b b (_)» tie on
    ///     «b b b a» with no name involved at all — a tie no bracketing repairs.
    /// </summary>
    private static IEnumerable<Finding> Anchors(IReadOnlyCollection<(Pattern Pattern, Span Span)> patterns)
    {
        foreach (var (pattern, span) in patterns)
        {
            foreach (var (other, elsewhere) in patterns)
            {
                if (ReferenceEquals(pattern, other)) continue;
                if (pattern.Anchor.Count >= other.Anchor.Count) continue;
                if (pattern.Anchor.SequenceEqual(other.Anchor.Take(pattern.Anchor.Count)) is false) continue;

                yield return new Finding(FindingKind.AnchorPrefix, elsewhere)
                    .Naming("pattern", other.ToString())
                    .Naming("prefix", pattern.ToString())
                    .Alongside(span, "the anchor this one begins with");
            }
        }
    }

    /// <summary>
    ///     One pattern using «old» as a segment would put it in the glue set, and
    ///     R5 would then reject every injected name in scope.
    /// </summary>
    private static IEnumerable<Finding> Reserved(IReadOnlyCollection<(Pattern Pattern, Span Span)> patterns)
    {
        foreach (var (pattern, span) in patterns)
        {
            if (pattern.Segments.Contains(SymbolTable.Old) is false) continue;

            yield return new Finding(FindingKind.ReservedSegment, span)
                .Naming("pattern", pattern.ToString())
                .Naming("word", SymbolTable.Old);
        }
    }

    /// <summary>
    ///     R5. A multi-word name may not contain pattern glue, or introducing a
    ///     name silently re-resolves statements that already worked.
    /// </summary>
    private static IEnumerable<Finding> Glue(IReadOnlyCollection<Declared> names,
                                             IReadOnlyCollection<(Pattern Pattern, Span Span)> patterns)
    {
        // A shadow is a multi-word name, so injected names are examined too, and
        // they must be: R5 never looks at a one-word declaration, so a collision
        // with «apply (_) smoothed (_)» is reachable ONLY through «old smoothed».
        var offending = names.ToDictionary(declared => declared.Name, declared => Offender(declared.Name, patterns));

        foreach (var declared in names.OrderBy(declared => declared.Name, System.StringComparer.Ordinal))
        {
            if (offending[declared.Name] is not (Pattern pattern, Span where)) continue;

            var word = pattern.Glue.First(declared.Name.Split(' ').Contains);

            if (declared.InjectedBy is null)
            {
                yield return new Finding(FindingKind.GlueInName, declared.Span)
                    .Naming("name", declared.Name)
                    .Naming("word", word)
                    .Naming("pattern", pattern.ToString())
                    .Alongside(where, "which makes it glue");

                continue;
            }

            // One mistake with one fix, so the shadow's complaint adds nothing.
            // Indexed rather than probed: whatever injects a name declares it
            // too, which is what «injected by» means.
            if (offending[declared.InjectedBy] is not null) continue;

            yield return new Finding(FindingKind.GlueInInjectedName, declared.Span)
                .Naming("name", declared.Name)
                .Naming("injector", declared.InjectedBy)
                .Naming("word", word)
                .Naming("pattern", pattern.ToString())
                .Alongside(where, "which makes it glue");
        }
    }

    private static (Pattern Pattern, Span Span)? Offender(string name, IReadOnlyCollection<(Pattern Pattern, Span Span)> patterns)
    {
        var words = name.Split(' ');
        if (words.Length < 2) return null;

        foreach (var candidate in patterns)
        {
            if (candidate.Pattern.Glue.Any(words.Contains)) return candidate;
        }

        return null;
    }
}
