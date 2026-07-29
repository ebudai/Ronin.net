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
/// <param name="Inherited">
///     Whether this came from an enclosing scope, which is the provenance the
///     rules need and the only kind available: the rules run over a merged
///     table, so both sides of a collision are simply "in scope" by the time they
///     meet. An enclosing declaration was written before anything nested inside
///     it, so this orders the two whenever they are in different scopes — and
///     within one scope, where they were written does.
/// </param>
internal readonly record struct Declared(string Name, Span Span, string InjectedBy = null, bool Inherited = false);

/// <summary>A pattern as declared, and where.</summary>
internal readonly record struct Shape(Pattern Pattern, Span Span, bool Inherited = false);

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
                                                IReadOnlyCollection<Shape> patterns)
    {
        foreach (var finding in Anchors(patterns)) yield return finding;
        foreach (var finding in Reserved(patterns)) yield return finding;
        foreach (var finding in Glue(names, patterns)) yield return finding;
    }

    /// <summary>
    ///     Whether the first of two declarations is the later one, which is the
    ///     one a message asks to give way.
    /// </summary>
    ///
    /// <remarks>
    ///     Every one of these rules names two declarations, and the caret used to
    ///     go on whichever the loop happened to hold — the name for R5, the longer
    ///     anchor for R6 — regardless of which was new. So a legal outer name
    ///     invalidated by an inner pattern reported the outer file, while the
    ///     message told the reader it was the later declaration that gives way.
    /// </remarks>
    private static bool IsLater(bool inherited, Span span, bool otherInherited, Span otherSpan)
        => inherited == otherInherited ? span.Offset > otherSpan.Offset : otherInherited;

    /// <summary>
    ///     R6. Anchor runs must be prefix free, or «b (_)» and «b b (_)» tie on
    ///     «b b b a» with no name involved at all — a tie no bracketing repairs.
    /// </summary>
    private static IEnumerable<Finding> Anchors(IReadOnlyCollection<Shape> patterns)
    {
        foreach (var shorter in patterns)
        {
            foreach (var longer in patterns)
            {
                if (ReferenceEquals(shorter.Pattern, longer.Pattern)) continue;
                if (shorter.Pattern.Anchor.Count >= longer.Pattern.Anchor.Count) continue;
                if (shorter.Pattern.Anchor.SequenceEqual(longer.Pattern.Anchor.Take(shorter.Pattern.Anchor.Count)) is false) continue;

                var later = IsLater(longer.Inherited, longer.Span, shorter.Inherited, shorter.Span) ? longer : shorter;
                var earlier = ReferenceEquals(later.Pattern, longer.Pattern) ? shorter : longer;

                yield return new AnchorPrefix(later.Span, longer.Pattern.ToString(), shorter.Pattern.ToString())
                    .Alongside(earlier.Span, "the anchor it collides with");
            }
        }
    }

    /// <summary>
    ///     One pattern using «old» as a segment would put it in the glue set, and
    ///     R5 would then reject every injected name in scope.
    /// </summary>
    private static IEnumerable<Finding> Reserved(IReadOnlyCollection<Shape> patterns)
    {
        foreach (var (pattern, span, _) in patterns)
        {
            if (pattern.Segments.Contains(SymbolTable.Old) is false) continue;

            yield return new ReservedSegment(span, pattern.ToString(), SymbolTable.Old);
        }
    }

    /// <summary>
    ///     R5. A multi-word name may not contain pattern glue, or introducing a
    ///     name silently re-resolves statements that already worked.
    /// </summary>
    private static IEnumerable<Finding> Glue(IReadOnlyCollection<Declared> names,
                                             IReadOnlyCollection<Shape> patterns)
    {
        // A shadow is a multi-word name, so injected names are examined too, and
        // they must be: R5 never looks at a one-word declaration, so a collision
        // with «apply (_) smoothed (_)» is reachable ONLY through «old smoothed».
        var offending = names.ToDictionary(declared => declared.Name, declared => Offender(declared.Name, patterns));

        foreach (var declared in names.OrderBy(declared => declared.Name, System.StringComparer.Ordinal))
        {
            if (offending[declared.Name] is not Shape offender) continue;

            var word = offender.Pattern.Glue.First(declared.Name.Split(' ').Contains);

            // Whichever was written later is the one being asked to give way, and
            // that is where the caret goes. An inner pattern can invalidate a
            // name declared in an enclosing scope, and blaming the outer file for
            // it is both wrong and unactionable — nothing in that file changed.
            var blamed = IsLater(declared.Inherited, declared.Span, offender.Inherited, offender.Span);

            var primary = blamed ? declared.Span : offender.Span;
            var related = blamed ? offender.Span : declared.Span;
            var label = blamed ? "which makes it glue" : "the name it collides with";

            // A name that is exactly the glue word, rather than one containing
            // it. Never injected: every injected name begins with «old », so it
            // has at least two words.
            if (declared.Name.Contains(' ') is false)
            {
                yield return new GlueAsName(primary, declared.Name, offender.Pattern.ToString())
                    .Alongside(related, label);

                continue;
            }

            // One mistake with one fix, so a shadow's complaint adds nothing —
            // and now it can never have one to add. A shadow offends only if the
            // name it was injected from does: «old X» contains glue that «X»
            // does not only when the glue word is «old», which Reserved already
            // refuses. Checking single-word names is what closed that gap; the
            // separate injected-name finding it used to need is gone with it.
            if (declared.InjectedBy is not null) continue;

            yield return new GlueInName(primary, declared.Name, word, offender.Pattern.ToString())
                .Alongside(related, label);
        }
    }

    /// <summary>
    ///     The first pattern whose glue this name contains.
    /// </summary>
    ///
    /// <remarks>
    ///     First and not all of them: a name colliding with three patterns is one
    ///     name to respell, and three findings saying so would be three copies of
    ///     one mistake. Repairing it can uncover the next, which is the accepted
    ///     cost — the alternative is a wall of messages with one fix between them.
    /// </remarks>
    private static Shape? Offender(string name, IReadOnlyCollection<Shape> patterns)
    {
        // Single-word names included. A name that IS a glue word is a different
        // finding from a name that CONTAINS one — the first is legibility, the
        // second is capture — but they are found the same way.
        var words = name.Split(' ');

        foreach (var candidate in patterns)
        {
            if (candidate.Pattern.Glue.Any(words.Contains)) return candidate;
        }

        return null;
    }
}
