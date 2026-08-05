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
///     One pattern being another with <paramref name="Word"/> and possibly more
///     words at the start of a hole, which reserves that word as a name prefix.
/// </summary>
internal readonly record struct Refinement(string Word, Shape Shorter, Shape Longer);

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
        var sound = patterns.Where(shape => Structural(shape.Pattern) is false).ToArray();

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
        foreach (var finding in Refining(names, sound)) yield return finding;
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
    ///     R7b. No name may begin with the word that tells one pattern from a
    ///     shorter one, where what follows is itself a name.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     «send (_) to all (_)» is «send (_) to (_)» with «all» at the start of
    ///     its second hole. A name «all things» then reads the whole of «send x
    ///     to all things» through the SHORTER pattern for what the longer one
    ///     costs reading it through «things» — a tie, at a call site, created by
    ///     a declaration somewhere else.
    ///     </para>
    ///     <para>
    ///     BLANKET, and the reason is which table the condition would be against
    ///     rather than a preference. Conditioning on the remainder resolving
    ///     makes a name's legality depend on the value language, which grows all
    ///     session — so a later declaration invalidates an earlier name, and the
    ///     convention refuses whichever arrived second:
    ///     </para>
    ///     <para>
    ///     <code>
    ///     var all things = …   legal, «things» is not declared yet
    ///     var things = …       refused — for a name far more natural than the
    ///                          one it collides with, about a variable its
    ///                          author may not own
    ///     </code>
    ///     </para>
    ///     <para>
    ///     Condition against a stable table, go blanket against a volatile one.
    ///     The article will be conditional for that reason and not this one:
    ///     types are few and declared early, so the re-check almost never fires.
    ///     </para>
    ///     <para>
    ///     A first attempt conditioned on the remainder being a DECLARED NAME,
    ///     which was too narrow twice over. What makes a second reading exist is
    ///     the remainder resolving as an expression, and a call is one:
    ///     «all count of items» is not a name, so the condition stayed silent —
    ///     and that case is worse than the tie, because the name is CHEAPER and
    ///     wins outright.
    ///     </para>
    ///     <code>
    ///     send x to all count of items   4 -> 3   resolved both ways, silently
    ///     send x to all things           3 -> 3   ambiguous, reported
    ///     </code>
    ///     <para>
    ///     The FIRST hole is R6's, not this: inserting there makes one anchor
    ///     run a prefix of the other and «sum of (_)» beside «sum of all (_)» is
    ///     refused before this runs. What is left for this is insertion at a
    ///     later hole, where the anchors are equal.
    ///     </para>
    ///     <para>
    ///     Patterns only, for now. The same relation runs over word operators —
    ///     «is» to «is not» is prefix extension of a word run — and there is no
    ///     multi-word operator in the tree to generate from, so that half
    ///     arrives with the machinery that makes one possible rather than as
    ///     code nothing can reach.
    ///     </para>
    /// </remarks>
    private static IEnumerable<Finding> Refining(IReadOnlyCollection<Declared> names,
                                                 IReadOnlyCollection<Shape> patterns)
    {
        // Derived ONCE and indexed by the word it reserves. It was recomputed
        // for every name against every ordered pair of patterns, which is cubic
        // in a scope and allocates on every comparison that fails — fifty names
        // and fifty patterns took 360 ms and 140 MB to report nothing at all.
        // The relation depends only on the pattern table, so a name has no
        // business being in the loop that computes it.
        var reserved = Refinements(patterns).ToLookup(refinement => refinement.Word, System.StringComparer.Ordinal);

        foreach (var name in names.OrderBy(name => name.Name, System.StringComparer.Ordinal))
        {
            if (name.InjectedBy is not null) continue;
            if (name.Words.Count < 2) continue;

            foreach (var (word, shorter, longer) in reserved[name.Words[0]])
            {
                // THREE declarations make this conflict, so the one asked to
                // give way is the latest of all three. Ordering the name against
                // the longer pattern alone blamed whichever of those two came
                // second even when the SHORTER pattern arrived after both and
                // was the thing that completed it.
                (bool Inherited, Span Span, Absorbing Role, string Label)[] parties =
                [
                    (name.Inherited, name.Span, Absorbing.Name, "the name that would absorb it"),
                    (shorter.Inherited, shorter.Span, Absorbing.Shorter, "the pattern it would be read through"),
                    (longer.Inherited, longer.Span, Absorbing.Longer, "the pattern it would absorb into"),
                ];

                var blamed = parties[0];

                foreach (var party in parties)
                {
                    if (IsLater(party.Inherited, party.Span, blamed.Inherited, blamed.Span)) blamed = party;
                }

                var finding = new NameAbsorbsRefinement(blamed.Span,
                                                        name.Name,
                                                        word,
                                                        shorter.Pattern.ToString(),
                                                        longer.Pattern.ToString(),
                                                        blamed.Role);

                foreach (var party in parties)
                {
                    if (party.Role != blamed.Role) finding.Alongside(party.Span, party.Label);
                }

                yield return finding;
            }
        }
    }

    /// <summary>
    ///     Every word that turns one pattern in this set into a longer one, with
    ///     the pair that reserves it.
    /// </summary>
    ///
    /// <remarks>
    ///     PUBLIC because the registry has to print it. R7b was a relationship
    ///     computed privately inside one rule, so the generated file that says
    ///     what the language reserves could not see it — and told a reader that
    ///     «all» is ordinary glue, free at an edge, while validation refused
    ///     every name beginning with it.
    /// </remarks>
    public static List<Refinement> Refinements(IReadOnlyCollection<Shape> patterns)
    {
        List<Refinement> found = [];

        foreach (var shorter in patterns)
        {
            foreach (var longer in patterns)
            {
                if (Refines(shorter.Pattern, longer.Pattern) is not string word) continue;

                found.Add(new Refinement(word, shorter, longer));
            }
        }

        return found;
    }

    /// <summary>
    ///     The word <paramref name="longer"/> inserts at the start of one of
    ///     <paramref name="shorter"/>'s holes, if that is all it does to it.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     The FIRST hole is skipped, and that is R6 rather than an optimisation:
    ///     inserting there extends the anchor run, so the shorter pattern's
    ///     anchor becomes a prefix of the longer's and R6 refuses the pair
    ///     outright. Reporting a name as well turns one structural mistake into
    ///     one finding per name in scope, all with the same repair — the
    ///     amplification the «sound» filter above exists to prevent elsewhere.
    ///     </para>
    ///     <para>
    ///     By INDEX and not by «Skip»/«Take»/«SequenceEqual». This runs once per
    ///     ordered pair of patterns, and the slices allocated on every pair that
    ///     did not match, which is almost all of them.
    ///     </para>
    /// </remarks>
    private static string Refines(Pattern shorter, Pattern longer)
    {
        var less = shorter.Segments;
        var more = longer.Segments;
        var run = more.Count - less.Count;

        if (run < 1) return null;

        for (var hole = shorter.Anchor.Count + 1; hole < less.Count; ++hole)
        {
            if (less[hole] is not null) continue;

            // The refined hole must be FREE. A pinned one takes exactly one word
            // or one bracketed name, so it cannot swallow the multi-word name
            // the rival reading needs — «send x to all things» has no reading
            // through «send (_) to «_»», because the pin takes «all» and leaves
            // «things» with nowhere to go. Reserving «all» there reserves a
            // prefix against an ambiguity that cannot happen.
            if (shorter.Pinned.Contains(hole)) continue;

            if (Alike(less, 0, more, 0, hole) is false) continue;
            if (Wordy(more, hole, run) is false) continue;
            if (more[hole + run] is not null) continue;
            if (Alike(less, hole + 1, more, hole + run + 1, less.Count - hole - 1) is false) continue;
            if (Pinned(shorter, longer, hole, run) is false) continue;

            return more[hole];
        }

        return null;
    }

    /// <summary>
    ///     Whether the two patterns pin the same holes, once the inserted run is
    ///     accounted for.
    /// </summary>
    ///
    /// <remarks>
    ///     Pinning is part of a pattern's identity, and the relation compared
    ///     only spellings — so two patterns that differ in what they pin looked
    ///     like one being the other plus a word. The hole the words go into
    ///     keeps its index in the shorter and gains the run in the longer;
    ///     everything before it keeps its index, everything after gains the run.
    /// </remarks>
    private static bool Pinned(Pattern shorter, Pattern longer, int hole, int run)
    {
        for (var at = 0; at < shorter.Segments.Count; ++at)
        {
            if (shorter.Segments[at] is not null) continue;

            // The refined hole itself is not compared. Its freedom in the
            // SHORTER is what the rival reading needs and is required above;
            // what the longer does with the hole it kept is its own business,
            // and both readings exist either way.
            if (at == hole) continue;

            if (shorter.Pinned.Contains(at) != longer.Pinned.Contains(at < hole ? at : at + run)) return false;
        }

        return true;
    }

    /// <summary>Whether two segment runs of <paramref name="count"/> match.</summary>
    private static bool Alike(IReadOnlyList<string> less, int from,
                              IReadOnlyList<string> more, int to, int count)
    {
        for (var at = 0; at < count; ++at)
        {
            // Holes match holes: «null» on both sides is one segment agreeing
            // with another, not two absences being confused.
            if (string.Equals(less[from + at], more[to + at], System.StringComparison.Ordinal) is false) return false;
        }

        return true;
    }

    /// <summary>Whether a run of segments is all words, with no hole among them.</summary>
    private static bool Wordy(IReadOnlyList<string> segments, int from, int count)
    {
        for (var at = 0; at < count; ++at)
        {
            if (segments[from + at] is null) return false;
        }

        return true;
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

            // INTERIOR only, which is R5′. An infix reading needs an operand on
            // each side, so a name can only be its rival where the word has
            // words on both sides of it — one that merely begins or ends with
            // the word has nothing on one side and cannot compete. The blanket
            // form refused «is valid» and «to uppercase», which is the name
            // shape a spaces-in-names grammar most encourages, while «time to
            // live» and «y is x» stay refused. The rule tracks the reading
            // rather than the spelling.
            if (Interior(declared.Words).FirstOrDefault(Infix.Contains) is not string word) continue;

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

            var wholly = Wholly(declared.Words, offender);
            var word = wholly ? declared.Words[0] : Interior(declared.Words).First(offender.Pattern.Glue.Contains);

            // Whichever was written later is the one being asked to give way, and
            // that is where the caret goes. An inner pattern can invalidate a
            // name declared in an enclosing scope, and blaming the outer file for
            // it is both wrong and unactionable — nothing in that file changed.
            var blamed = IsLater(declared.Inherited, declared.Span, offender.Inherited, offender.Span);

            var primary = blamed ? declared.Span : offender.Span;
            var related = blamed ? offender.Span : declared.Span;
            var label = blamed ? "which makes it glue" : "the name it collides with";

            // A name made only of glue, of whatever length — one rule and one
            // reason, two arities. Never injected: an injected name would have
            // to be wholly glue, which needs «old» to be glue, and a pattern
            // using it that way is structurally refused before this runs.
            if (wholly)
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
        // Two clauses, and both are capture. INTERIOR glue, because a name can
        // only re-read a call it spans and it needs a word on each side of the
        // glue to do that. And WHOLLY glue, because a name made only of glue
        // words has none interiorly and still captures — «to to» beside «to»
        // gives «send to to to to» two readings at the same cost, one with the
        // literal at each viable position, and the statement becomes unwritable.
        foreach (var candidate in patterns)
        {
            if (Wholly(words, candidate) || Interior(words).Any(candidate.Pattern.Glue.Contains)) return candidate;
        }

        return null;
    }

    /// <summary>Whether every word of the name is glue in this pattern.</summary>
    private static bool Wholly(IReadOnlyList<string> words, Shape candidate)
        => words.All(candidate.Pattern.Glue.Contains);
}
