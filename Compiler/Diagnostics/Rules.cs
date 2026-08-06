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
///     change, since «index of bank» is not the programmer's to rename.
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
internal readonly record struct Shape(Pattern Pattern, Span Span, bool Inherited = false, bool Builtin = false);

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
        // words. It is still examined for its own structural finding, but not
        // allowed to amplify that mistake into a complaint against every name
        // or pattern beside it.
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
        foreach (var finding in Injecting(patterns)) yield return finding;

        // What a pattern does to something else, asked only of the sound ones.
        // The infix findings are held rather than streamed because the shadowing
        // rule needs to know which written names have been blamed already: a
        // name the compiler copies into one it builds carries its offence along,
        // and the copy is not a second mistake.
        var infixes = Infixes(names).ToArray();

        foreach (var finding in infixes) yield return finding;
        foreach (var finding in Anchors(sound)) yield return finding;
        foreach (var finding in Shadowing(names, sound, infixes.Select(finding => finding.Name))) yield return finding;
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
        => pattern.Segments.Any(Infix.Contains)
        || Injected.Any(injection => pattern.Glue.Contains(injection.Word));

    /// <summary>
    ///     A name may not have another reading over its own complete span. One
    ///     such reading is a pattern whose holes can consume all of the name's
    ///     remaining words.
    /// </summary>
    ///
    /// <remarks>
    ///     THE PRE-TYPE-CHECKER FORM of a narrower rule, and this paragraph is
    ///     the expiry rather than the justification. What makes a second reading
    ///     fatal is that nothing can eliminate it: brackets group and do not
    ///     classify, so «print (job)» is still the call and the name reading has
    ///     no spelling. Well-typedness does classify, and eliminating by it is
    ///     not a silent pick — so once types exist this shrinks to
    ///     <em>a name may not have another reading of the same type in the same
    ///     position</em>, and what is left is only a name declared with the
    ///     return type of the call it swallows.
    ///     <para>
    ///     Which is most of what this refuses today. Not to be relaxed before
    ///     then: between here and there, relaxing it makes programs unwritable
    ///     with no diagnostic, and that is the one outcome worse than a refused
    ///     name. <c>Test.Expiry</c> tags which fixtures go and which stay.
    ///     </para>
    /// </remarks>
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
    ///     Glued patterns are included. The deleted blanket glue rule correctly
    ///     made «a to b» legal, but it also removed the narrower own-span case:
    ///     «send x to y» itself reads as «send (_) to (_)», and no bracket
    ///     selects the name. Testing the complete span states that boundary
    ///     directly instead of approximating it by an anchor prefix.
    ///     </para>
    /// </remarks>
    private static IEnumerable<Finding> Shadowing(IReadOnlyCollection<Declared> names,
                                                  IReadOnlyCollection<Shape> patterns,
                                                  IEnumerable<string> refused)
    {
        // A literal-only pattern has no application over a name. Every pattern
        // with a hole can have one, whether its words are all in the anchor or
        // separated by glue.
        var exposed = patterns.Where(shape => shape.Pattern.Segments.Contains(null)).ToArray();

        // GENERATED names are asked too, which they were not. The skip read
        // "not the programmer's to rename, and its origin is reported already" —
        // true of the rename and false of the reporting: nothing else reports
        // this, so a pattern «index of (_)» beside any loop at all was shadowed
        // by the counter that loop generates, silently and with nothing refused.
        var collisions =
            (from declared in names.OrderBy(declared => declared.Name, System.StringComparer.Ordinal)
             from shape in exposed
             where ReadsAs(declared.Words, shape.Pattern)
             select (Declared: declared, Shape: shape)).ToArray();

        // Every written name a name rule has already blamed. The compiler copies
        // one of these into each name it builds from it, so the built name
        // offends for the same reason and by the same words — one mistake, and
        // the rename that answers it answers both.
        var offending = new System.Collections.Generic.HashSet<string>(
            refused.Concat(collisions.Where(collision => collision.Declared.InjectedBy is null)
                                     .Select(collision => collision.Declared.Name)),
            System.StringComparer.Ordinal);

        foreach (var (declared, shape) in collisions)
        {
            // A UNIVERSAL collision is the pattern's alone: its words end inside
            // the prefix the compiler adds, so every name built by that
            // injection begins with them and no rename anyone can perform
            // avoids it. Naming one generated example instead reported the same
            // pattern once per loop in scope, each message differing only in the
            // subject it interpolated and each asking for the same one edit.
            if (Universal(declared, shape.Pattern))
            {
                yield return new NameShadowsPattern(shape.Span, Built(declared), shape.Pattern.ToString(), universal: true);
                continue;
            }

            // ONE MISTAKE, so one diagnostic. A built name whose subject was
            // blamed on its own account offends by the words it was given, and
            // the rename already asked for is the whole repair — reporting the
            // copy as well is a second message about a name nobody wrote.
            if (declared.InjectedBy is not null && offending.Contains(declared.InjectedBy)) continue;

            // PARTICULAR, so both parties are actionable — the pattern can be
            // respelled and the name the subject was copied from can be renamed
            // — and the standing convention decides between them. A generated
            // name used to lose this ordering whenever it was written, which
            // pointed at a pattern that was correct when it was declared and
            // asked for a larger change than the one that fixes it.
            var blamed = IsLater(declared.Inherited, declared.Span, shape.Inherited, shape.Span);

            var finding = new NameShadowsPattern(blamed ? declared.Span : shape.Span,
                                                 declared.Name,
                                                 shape.Pattern.ToString(),
                                                 declared.InjectedBy,
                                                 builtin: shape.Builtin);

            // A built-in has no source declaration. Its zero-width bookkeeping
            // span is not a place a diagnostic may point.
            if (shape.Builtin is false)
                finding.Alongside(blamed ? shape.Span : declared.Span,
                                  blamed ? "the pattern it would shadow" : "the name that would shadow it");

            yield return finding;
        }
    }

    /// <summary>
    ///     Whether a name's complete words can also be a call to
    ///     <paramref name="pattern"/>. A free hole consumes one or more possible
    ///     name words; a pinned hole consumes exactly one in an unbracketed name.
    /// </summary>
    private static bool ReadsAs(IReadOnlyList<string> words, Pattern pattern)
    {
        bool[,] answer = new bool[pattern.Segments.Count + 1, words.Count + 1];
        bool[,] known = new bool[pattern.Segments.Count + 1, words.Count + 1];

        bool Read(int segment, int word)
        {
            if (known[segment, word]) return answer[segment, word];
            known[segment, word] = true;

            if (segment == pattern.Segments.Count)
                return answer[segment, word] = word == words.Count;

            if (pattern.Segments[segment] is string literal)
                return answer[segment, word] = word < words.Count
                                              && words[word] == literal
                                              && Read(segment + 1, word + 1);

            if (pattern.Pinned.Contains(segment))
                return answer[segment, word] = word < words.Count && Read(segment + 1, word + 1);

            for (var after = word + 1; after <= words.Count; ++after)
                if (Read(segment + 1, after)) return answer[segment, word] = true;

            return false;
        }

        return Read(0, 0);
    }

    /// <summary>
    ///     Whether a built name's collision holds for every name it could have
    ///     been built from, rather than for the one it was.
    /// </summary>
    ///
    /// <remarks>
    ///     The test is to substitute a fresh, otherwise-unused subject and ask
    ///     again. A built name is a fixed prefix plus a copied subject, and the
    ///     substitution is asked through the same complete-span predicate as
    ///     the actual collision so glued patterns cannot be misclassified.
    ///     <para>
    ///     A written name is never universal. There is no prefix the compiler
    ///     chose and no hole to substitute into, so the collision is exactly as
    ///     particular as the name is.
    ///     </para>
    /// </remarks>
    private static bool Universal(Declared declared, Pattern pattern)
        => declared.InjectedBy is not null
        && ReadsAs(Injection.All.First(injection => declared.Words.Take(injection.Words.Count)
                                                            .SequenceEqual(injection.Words))
                                      .Of(["a-fresh-name"]),
                   pattern);

    /// <summary>How the compiler describes what it builds, when no subject is to blame.</summary>
    private static string Built(Declared declared)
        => Injection.All.First(injection => declared.Words.Take(injection.Words.Count).SequenceEqual(injection.Words))
                        .Shape;

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
    ///     BUILT names are asked too, and were exempt. «for each (is valid) in …»
    ///     builds «index of is valid», whose interior spans the operator — so
    ///     the name won the comparison its author wrote, and nothing said so.
    ///     The counter injection contributes only «index of», neither of which
    ///     is an operator, so what collides always came from the subject and a
    ///     rename always answers it. There is no case here where the collision
    ///     holds whatever the subject is, which is why this rule needs no
    ///     counterpart to the universal split next door.
    ///     </para>
    ///     <para>
    ///     Which leaves the operator as the other party, and it cannot be
    ///     respelled — so the originating name is blamed rather than the built
    ///     one, and the ordering convention never runs.
    ///     </para>
    ///     <para>
    ///     THE PRE-TYPE-CHECKER FORM, on the same expiry as its other half next
    ///     door: a comparison is a truth whatever its operands are, so a name
    ///     spanning one is eliminated by type unless it is declared a truth
    ///     itself. What survives is «a name that spans a comparison operator and
    ///     is itself a truth» — a boolean called «y is x» sitting beside the
    ///     comparison «y is x», where the reader cannot tell them apart either.
    ///     </para>
    /// </remarks>
    private static IEnumerable<InfixInName> Infixes(IReadOnlyCollection<Declared> names)
    {
        // ONE MISTAKE, one diagnostic. A loop subject can offend on its own and
        // again inside the counter built from it, by the same word for the same
        // reason with one rename between them.
        var offending = new HashSet<string>(
            names.Where(declared => declared.InjectedBy is null)
                 .Where(declared => Interior(declared.Words).Any(Infix.Contains))
                 .Select(declared => declared.Name),
            System.StringComparer.Ordinal);

        foreach (var declared in names.OrderBy(declared => declared.Name, System.StringComparer.Ordinal))
        {
            if (declared.InjectedBy is not null && offending.Contains(declared.InjectedBy)) continue;
            if (Interior(declared.Words).FirstOrDefault(Infix.Contains) is not string word) continue;

            yield return new InfixInName(declared.Span, declared.InjectedBy ?? declared.Name, word, declared.InjectedBy is not null);
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
        foreach (var (pattern, span, _, _) in patterns)
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
    ///     Refused as GLUE only, because they are ordinary words in anchor
    ///     position and the language wants them there — «sum of (_)» and
    ///     «count of (_)» are the shapes to prefer, and banning «of» outright
    ///     would take them away. «old» is absent: it names a pattern now and
    ///     injects no source-level symbol.
    /// </remarks>
    public static IReadOnlyList<(string Word, string Injects)> Injected { get; } =
        [.. Injection.All.SelectMany(injection => injection.Words.Select(word => (word, injection.Shape)))];

    /// <summary>
    ///     Injection words may not be glue. The dual of glue words not being
    ///     names, and it closes the trap in the other direction.
    /// </summary>
    private static IEnumerable<Finding> Injecting(IReadOnlyCollection<Shape> patterns)
    {
        foreach (var (pattern, span, _, _) in patterns)
        {
            foreach (var (word, injects) in Injected)
            {
                if (pattern.Glue.Contains(word) is false) continue;

                yield return new InjectionWordAsGlue(span, pattern.ToString(), word, injects);
            }
        }
    }

}
