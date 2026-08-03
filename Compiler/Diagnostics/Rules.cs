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
internal readonly record struct Declared(string Name, Span Span, string InjectedBy = null, bool Inherited = false)
{
    /// <summary>
    ///     The name as the lexer counts its words, which is the sequence every
    ///     rule about words has to ask.
    /// </summary>
    ///
    /// <remarks>
    ///     <see cref="Name"/> is a RENDERING of this, and R5 used to take that
    ///     rendering apart on spaces — so a glue segment «part of» was compared
    ///     against the two words «part» and «of», matched neither, and a name
    ///     containing the glue was declared with no finding at all. That is the
    ///     rule which exists to stop silent capture.
    ///     <para>
    ///     Set from the declaration's own tokens where there is one, and
    ///     otherwise recovered by LEXING the rendering — which is the same answer
    ///     for every name a rendering can express, and is not the operation that
    ///     was wrong. Splitting on spaces was.
    ///     </para>
    /// </remarks>
    public IReadOnlyList<string> Words
    {
        get => words ?? Lexemes.Words(Name);
        init => words = value;
    }

    private readonly IReadOnlyList<string> words;
}

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
        foreach (var finding in Infixes(names)) yield return finding;
        foreach (var finding in Shadowing(names, patterns)) yield return finding;
        foreach (var finding in Reserved(patterns)) yield return finding;
        foreach (var finding in Injecting(patterns)) yield return finding;

        // A pattern that is structurally wrong does not then get to reserve
        // words. «recall (_) old (_)» is refused once for using «old» as a
        // segment — and it was ALSO run through the name scan, so every mutable
        // declaration in the file collected its own complaint about the shadow
        // «old» had just invalidated. Three variables, three extra findings; a
        // hundred, a hundred. Every one of them had the same repair as the
        // first, which is the thing the structural finding already says.
        var sound = patterns.Where(shape => Structural(shape.Pattern) is false).ToArray();

        foreach (var finding in Glue(names, sound)) yield return finding;
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
    ///     Whether a pattern is wrong in itself, rather than in company. One of
    ///     these has been reported already and its repair is a respelling, so
    ///     letting it go on to reserve words produces a second complaint the
    ///     first one covers.
    /// </summary>
    private static bool Structural(Pattern pattern)
        => pattern.Segments.Contains(SymbolTable.Old)
        || Injected.Any(injection => pattern.Glue.Contains(injection.Word));

    /// <summary>
    ///     R6b. No name may have a pattern's whole word content as a proper
    ///     prefix, or it is read instead of the call and more cheaply.
    /// </summary>
    ///
    /// <remarks>
    ///     Glue-free patterns only. One with glue needs its glue word inside any
    ///     name that could reach the whole call, and R5 has refused that already
    ///     — asking both would be two findings for one repair, which is what the
    ///     structural guard below exists to avoid elsewhere.
    ///     <para>
    ///     PROPER prefix, so a name equal to the pattern's words is left alone.
    ///     It cannot capture: the call's argument would have to sit beside it as
    ///     a second juxtaposed name, and that is not an expression.
    ///     </para>
    /// </remarks>
    private static IEnumerable<Finding> Shadowing(IReadOnlyCollection<Declared> names,
                                                  IReadOnlyCollection<Shape> patterns)
    {
        // Anchor-only, which is not the same as glue-free: a pinned hole makes
        // the word after it free of glue and still leaves the words apart, so
        // there is no contiguous run for a name to begin with.
        var exposed = patterns.Where(shape => shape.Pattern.IsAnchorOnly).ToArray();

        foreach (var declared in names.OrderBy(declared => declared.Name, System.StringComparer.Ordinal))
        {
            // Not the programmer's to rename, and its origin is reported already.
            if (declared.InjectedBy is not null) continue;

            foreach (var shape in exposed)
            {
                var words = shape.Pattern.Segments.Where(segment => segment is not null).ToArray();

                if (declared.Words.Count <= words.Length) continue;
                if (declared.Words.Take(words.Length).SequenceEqual(words) is false) continue;

                var blamed = IsLater(declared.Inherited, declared.Span, shape.Inherited, shape.Span);

                yield return new NameShadowsPattern(blamed ? declared.Span : shape.Span,
                                                   declared.Name,
                                                   shape.Pattern.ToString())
                    .Alongside(blamed ? shape.Span : declared.Span,
                               blamed ? "the pattern it would shadow" : "the name that would shadow it");
            }
        }
    }

    /// <summary>
    ///     The words the language reads as operators between two values.
    /// </summary>
    ///
    /// <remarks>
    ///     Read from the one operator table rather than listed again, so a word
    ///     operator added there is reserved here without anyone remembering to.
    ///     Symbols are excluded because no name can contain one — a name is a
    ///     run of WORDS, which is the whole reason a symbolic infix costs
    ///     nothing and a word one costs a word.
    /// </remarks>
    public static IReadOnlyList<string> Infix { get; }
        = [.. Runtime.Builtin.Operators.Keys.Where(word => word.All(char.IsLetter))
                                            .OrderBy(word => word, System.StringComparer.Ordinal)];

    /// <summary>
    ///     A name may not contain an operator word, for R5's reason against a
    ///     different rival.
    /// </summary>
    private static IEnumerable<Finding> Infixes(IReadOnlyCollection<Declared> names)
    {
        foreach (var declared in names.OrderBy(declared => declared.Name, System.StringComparer.Ordinal))
        {
            // An injected name is not the programmer's to rename, and none is
            // built from an operator word — «old» and «index of» are the whole
            // set — so one arriving here would be a defect in the injector
            // rather than something to ask anyone about.
            if (declared.InjectedBy is not null) continue;

            if (declared.Words.FirstOrDefault(Infix.Contains) is not string word) continue;

            yield return new InfixInName(declared.Span, declared.Name, word);
        }
    }

    /// <summary>
    ///     The words the compiler builds injected names from, and the name each
    ///     one builds.
    /// </summary>
    ///
    /// <remarks>
    ///     «old» is refused as any segment by <see cref="Reserved"/>, which is
    ///     stricter and stays. These are refused as GLUE only, because they are
    ///     ordinary words in anchor position and the language wants them there —
    ///     «sum of (_)» and «count of (_)» are the shapes to prefer, and banning
    ///     «of» outright would take them away.
    /// </remarks>
    public static IReadOnlyList<(string Word, string Injects)> Injected { get; } =
        [.. Injection.All.Where(injection => injection != Injection.Shadow)
                         .SelectMany(injection => injection.Words.Select(word => (word, injection.Shape)))];

    /// <summary>
    ///     Injection words may not be glue. The dual of glue words not being
    ///     names, and it closes the trap in the other direction.
    /// </summary>
    private static IEnumerable<Finding> Injecting(IReadOnlyCollection<Shape> patterns)
    {
        foreach (var (pattern, span, _) in patterns)
        {
            foreach (var (word, injects) in Injected)
            {
                if (pattern.Glue.Contains(word) is false) continue;

                yield return new InjectionWordAsGlue(span, pattern.ToString(), word, injects);
            }
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
        // Grouped rather than keyed directly: a name reaching here twice is a
        // defect upstream, and dying on it here reports the defect instead of
        // the collision the caller was asking about.
        var offending = names.GroupBy(declared => declared.Name)
                             .ToDictionary(declared => declared.Key,
                                           declared => Offender(declared.First().Words, patterns));

        foreach (var declared in names.OrderBy(declared => declared.Name, System.StringComparer.Ordinal))
        {
            if (offending[declared.Name] is not Shape offender) continue;

            var word = offender.Pattern.Glue.First(declared.Words.Contains);

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

            // An injected name never complains on its own. It offends only if
            // the name it came from does — «old X» and «index of X» add «old»,
            // «index» and «of», and a pattern making any of those glue is
            // structurally invalid and was excluded above — so the injector's
            // finding is always there and always has the same repair.
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
    private static Shape? Offender(IReadOnlyList<string> words, IReadOnlyCollection<Shape> patterns)
    {
        // Single-word names included. A name that IS a glue word is a different
        // finding from a name that CONTAINS one — the first is legibility, the
        // second is capture — but they are found the same way.
        foreach (var candidate in patterns)
        {
            if (candidate.Pattern.Glue.Any(words.Contains)) return candidate;
        }

        return null;
    }
}
