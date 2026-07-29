// Copyright © 2026 Eric Budai

using System;
using System.Collections.Generic;
using System.Linq;

namespace Ronin.Compiler;

/// <summary>
///     What could continue a statement that is still being typed.
/// </summary>
///
/// <remarks>
///     <para>
///     <see cref="Resolver"/> scores a finished statement. This answers the
///     writer's question instead — the words so far are a prefix of what, and
///     what comes next? It does not score anything, because a half-typed
///     statement has no cost to compare: every continuation is offered and the
///     resolver decides once the statement is complete.
///     </para>
///     <para>
///     Names and pattern anchors are both runs of consecutive words, so only a
///     suffix of the trailing word run can be partway through one. Every suffix
///     is tried, longest first, because the earlier words may belong to
///     something already finished: after «send hello», «hello» is a complete
///     argument to «send _» and also a prefix of the name «hello to alice».
///     </para>
/// </remarks>
internal sealed class Completion
{
    public Completion(SymbolTable symbols)
    {
        ArgumentNullException.ThrowIfNull(symbols);

        this.symbols = symbols;
    }

    /// <summary>
    ///     Every word that could come next, most specific first. A candidate that
    ///     continues four typed words outranks one that continues none, which is
    ///     what keeps the list readable — the tail of it is always "or start
    ///     something new".
    /// </summary>
    public IReadOnlyList<Candidate> After(IReadOnlyList<Lexeme> lexemes)
    {
        ArgumentNullException.ThrowIfNull(lexemes);

        var typed = TrailingWords(lexemes);

        // Once per request, not once per suffix. A name's words do not depend on
        // how much of it has been typed, and lexing inside both loops built a
        // lexer, a token chain and an array «(typed + 1) × names» times — on an
        // editor's keystroke path.
        var declared = new (string Name, string[] Words)[symbols.Names.Count];

        var at = 0;
        foreach (var name in symbols.Names) declared[at++] = (name, Lexemes.Words(name));

        List<Candidate> candidates = [];
        HashSet<(CandidateKind, string, string)> seen = [];

        for (var start = 0; start <= typed.Length; ++start)
        {
            var partial = typed[start..];

            foreach (var (name, words) in declared)
            {
                if (Continues(words, partial) is not string word) continue;
                if (seen.Add((CandidateKind.Name, word, name)))
                    candidates.Add(new Candidate(CandidateKind.Name, word, name, partial.Length, words.Length));
            }

            foreach (var pattern in symbols.Patterns)
            {
                if (Continues(pattern.Anchor, partial) is not string word) continue;

                var whole = pattern.ToString();
                if (seen.Add((CandidateKind.Pattern, word, whole)))
                    candidates.Add(new Candidate(CandidateKind.Pattern, word, whole, partial.Length, pattern.Anchor.Count));
            }
        }

        // Longest first within a rank, because that is the resolver's own bias:
        // cost is lookups, so «base price» is one where «base» «price» is two,
        // and the greedier reading wins. Ordering the list the same way teaches
        // the bias instead of hiding it behind the alphabet.
        //
        // SymbolTable.Names is a set and Patterns is unordered against it, so the
        // last two keys are not decoration — without them the same keystroke
        // offers a different order each run.
        return [.. candidates.OrderByDescending(candidate => candidate.Matched)
                             .ThenByDescending(candidate => candidate.Words)
                             .ThenBy(candidate => candidate.Word, StringComparer.Ordinal)
                             .ThenBy(candidate => candidate.Whole, StringComparer.Ordinal)];
    }

    /// <summary>
    ///     The word that would extend <paramref name="partial"/> one step toward
    ///     <paramref name="whole"/>, or null when it is not on the way there.
    /// </summary>
    private static string Continues(IReadOnlyList<string> whole, IReadOnlyList<string> partial)
    {
        if (whole.Count <= partial.Count) return null;

        for (var i = 0; i != partial.Count; ++i)
        {
            if (whole[i] != partial[i]) return null;
        }

        return whole[partial.Count];
    }

    /// <summary>
    ///     The run of words the caret is sitting at the end of. A symbol, bracket
    ///     or literal ends it, since nothing spanning one can be a single name.
    /// </summary>
    private static string[] TrailingWords(IReadOnlyList<Lexeme> lexemes)
    {
        var start = lexemes.Count;
        while (start > 0 && lexemes[start - 1].Kind is LexemeKind.Word) --start;

        var words = new string[lexemes.Count - start];
        for (var i = 0; i != words.Length; ++i) words[i] = lexemes[start + i].Text;
        return words;
    }

    private readonly SymbolTable symbols;
}

internal enum CandidateKind { Name, Pattern }

/// <summary>
///     One word that could come next, and what it would be part of.
/// </summary>
///
/// <param name="Word">The word to type.</param>
/// <param name="Whole">The name or pattern it belongs to, for the writer to read.</param>
/// <param name="Matched">How many already-typed words it continues.</param>
/// <param name="Words">
///     How many literal words the whole commits — every word of a name, and a
///     pattern's anchor, whose holes swallow spans that cannot be counted here.
/// </param>
internal readonly record struct Candidate(CandidateKind Kind, string Word, string Whole, int Matched, int Words);
