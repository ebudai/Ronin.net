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
        // BOTH ways in, by the one ownership rule. The split gives an array,
        // which reports «IsReadOnly» as true and still assigns through a cast;
        // what «init» receives is whatever object a caller built, and the words
        // a diagnostic rule reads should not change afterwards because that
        // caller kept a reference.
        get => words ?? Owned.Copy(Lexemes.Words(Name));
        init => words = Owned.Copy(value);
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
        // A pattern that is structurally wrong does not then get to reserve
        // words. «recall (_) old (_)» is refused once for using «old» as a
        // segment — and it was ALSO run through the name scan, so every mutable
        // declaration in the file collected its own complaint about the shadow
        // «old» had just invalidated. Three variables, three extra findings; a
        // hundred, a hundred. Every one of them had the same repair as the
        // first, which is the thing the structural finding already says.
        //
        // Computed FIRST, and applied to every relational rule rather than to
        // the glue scan alone. That comment stated the invariant for all of
        // them and only one of them obeyed it, so an invalid pattern went on
        // reserving prefixes through R6b and refinements through R7 — the same
        // amplification, by a different door.
        var sound = patterns.Where(shape => Sound(shape.Pattern)).ToArray();

        // What a pattern is wrong about IN ITSELF, asked of all of them: these
        // are the findings that make a pattern unsound, so filtering their input
        // by soundness would be asking a pattern to report itself.
        foreach (var finding in Infixes(patterns)) yield return finding;
        foreach (var finding in Reserved(patterns)) yield return finding;
        foreach (var finding in Injecting(patterns)) yield return finding;

        // What a pattern does to something else, asked only of the sound ones.
        foreach (var finding in Infixes(names)) yield return finding;
        foreach (var finding in Anchors(sound)) yield return finding;
        foreach (var finding in Shadowing(names, sound)) yield return finding;
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
    ///     Whether a pattern is legal in itself, and so allowed to reserve
    ///     anything against anyone.
    /// </summary>
    ///
    /// <remarks>
    ///     PUBLIC because the registry has to ask it too. The predicate was
    ///     private here while «Glue.Registry» built its tables from every
    ///     pattern it was given, so the generated file could publish a
    ///     reservation from a pattern the compiler refuses to admit — a
    ///     breaking-change report about a relationship that cannot exist.
    /// </remarks>
    public static bool Sound(Pattern pattern) => Structural(pattern) is false;

    /// <summary>
    ///     Whether a pattern is wrong in itself, rather than in company. One of
    ///     these has been reported already and its repair is a respelling, so
    ///     letting it go on to reserve words produces a second complaint the
    ///     first one covers.
    /// </summary>
    private static bool Structural(Pattern pattern)
        => pattern.Segments.Contains(SymbolTable.Old)
        || pattern.Segments.Any(Infix.Contains)
        || Injected.Any(injection => pattern.Glue.Contains(injection.Word));

    /// <summary>
    ///     R6b. No name may have a pattern's whole word content as a proper
    ///     prefix, or it is read instead of the call and more cheaply.
    /// </summary>
    ///
    /// <remarks>
    ///     WITHIN A MODULE, and this is the place that will be got wrong. R5 and
    ///     this are blanket declaration-time rules: they refuse a name for what
    ///     it is spelled, without asking whether a rival reading exists. That
    ///     trade is paid for by the repair being a rename, and inside one module
    ///     the author owns both sides.
    ///     <para>
    ///     Across an import boundary they own neither, and the blanket rule
    ///     over-refuses by about 87% — «hello to», «send to», «print print» can
    ///     never capture anything and would each make two innocent libraries
    ///     unusable together, with no rename available to anyone. So imported
    ///     symbols must NOT arrive here marked <c>Inherited</c> the way an
    ///     enclosing scope's do. The boundary wants a differential check —
    ///     whether an import changes the reading of a statement the importing
    ///     module already had — which is exactly as strict as the danger and
    ///     nothing more.
    ///     </para>
    ///     <para>
    ///     Glue-free patterns only. One with glue needs its glue word inside any
    ///     name that could reach the whole call, and R5 has refused that already
    ///     — asking both would be two findings for one repair, which is what the
    ///     structural guard below exists to avoid elsewhere.
    ///     </para>
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
            // GENERATED names are asked too, which they were not. The skip read
            // "not the programmer's to rename, and its origin is reported
            // already" — true of the rename and false of the reporting: nothing
            // else reports this, so a pattern «index of (_)» beside any loop at
            // all was shadowed by the counter that loop generates, silently and
            // with nothing refused.
            foreach (var shape in exposed)
            {
                var words = shape.Pattern.Segments.Where(segment => segment is not null).ToArray();

                if (declared.Words.Count <= words.Length) continue;
                if (declared.Words.Take(words.Length).SequenceEqual(words) is false) continue;

                // A generated name always loses the ordering, whenever it was
                // written. The convention asks the LATER declaration to give
                // way because the earlier author cannot have known — and here
                // neither author can, since the name has no spelling anyone
                // chose. What is left to change is the pattern.
                var blamed = declared.InjectedBy is null
                          && IsLater(declared.Inherited, declared.Span, shape.Inherited, shape.Span);

                yield return new NameShadowsPattern(blamed ? declared.Span : shape.Span,
                                                   declared.Name,
                                                   shape.Pattern.ToString(),
                                                   declared.InjectedBy)
                    .Alongside(blamed ? shape.Span : declared.Span,
                               blamed ? "the pattern it would shadow" : "the name that would shadow it");
            }
        }
    }

    /// <summary>
    ///     Half of R5′, and one of the two ways a name's own span reads as
    ///     something else: it spans an infix operator.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     KEPT where the glue half went, and the difference is repairability.
    ///     «a to b» reads only as itself — the ambiguity it causes is in some
    ///     other statement, and a bracket there reaches it. «x is y» reads as a
    ///     comparison of its own words, so no bracketing selects the name: the
    ///     declaration would be unwriteable.
    ///     </para>
    ///     <para>
    ///     INTERIOR only. An infix reading needs an operand on each side, so a
    ///     name that merely begins or ends with the word has nothing on one side
    ///     and cannot compete — «is valid» is legal and «y is x» is not.
    ///     </para>
    ///     <para>
    ///     GENERATED names are still skipped, and that is the open half. The
    ///     design says there is no exemption — an injected span containing an
    ///     operator word is self-ambiguous like any other, and «for each (is
    ///     valid) in …» generates «index of is valid», which captured a
    ///     comparison its author wrote.
    ///     </para>
    ///     <para>
    ///     Removing the skip on its own DOUBLES the report: a name that offends
    ///     and its «old» shadow both say so, with one repair between them. The
    ///     settled rule is three rows — suppress the shadow when the SOURCE also
    ///     fails, blame the source when only the injection fails and a rename
    ///     would help, blame the pattern once when no rename would — and the
    ///     first row is what makes removing this skip add no messages at all.
    ///     </para>
    ///     <para>
    ///     BLOCKED ON «REAUDIT46» findings 2 and 3, which are that machinery.
    ///     Named here so the two halves find each other; this is the open half
    ///     of a rule and not a reason the skip is here.
    ///     </para>
    ///     <para>
    ///     Safe to wait, and worth saying why. Under minimum lookup an exempted
    ///     injected name was a real hazard — a self-ambiguous span resolved
    ///     silently to whichever reading was cheaper. Ambiguity is the error
    ///     now, so any span with two readings fails at the use site whatever is
    ///     in the table: this can produce a confusing message, not a wrong
    ///     reading. Diagnostics debt rather than soundness debt.
    ///     </para>
    /// </remarks>
    private static IEnumerable<Finding> Infixes(IReadOnlyCollection<Declared> names)
    {
        foreach (var declared in names.OrderBy(declared => declared.Name, System.StringComparer.Ordinal))
        {
            if (declared.InjectedBy is not null) continue;
            if (Interior(declared.Words).FirstOrDefault(Infix.Contains) is not string word) continue;

            yield return new InfixInName(declared.Span, declared.Name, word);
        }
    }

    /// <summary>
    ///     The words of <paramref name="words"/> that have a word on each side.
    /// </summary>
    ///
    /// <remarks>
    ///     «Take» of a negative count is empty rather than an error, so a name
    ///     of one word needs no case of its own.
    /// </remarks>
    private static IEnumerable<string> Interior(IReadOnlyList<string> words)
        => words.Skip(1).Take(words.Count - 2);

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
    ///     Nor may a pattern use one, which fails the other way.
    /// </summary>
    ///
    /// <remarks>
    ///     A name is cheaper than the expression it covers and wins silently; a
    ///     pattern costs exactly what the operator costs and ties. So this is an
    ///     ambiguity rather than a capture — reported at every call site, far
    ///     from the declaration that caused it, which is why the declaration is
    ///     what gets refused.
    /// </remarks>
    private static IEnumerable<Finding> Infixes(IReadOnlyCollection<Shape> patterns)
    {
        foreach (var (pattern, span, _) in patterns)
        {
            if (pattern.Segments.FirstOrDefault(Infix.Contains) is not string word) continue;

            yield return new InfixInPattern(span, pattern.ToString(), word);
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

}
