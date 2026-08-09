// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using System.Collections.Generic;
using System.Linq;

namespace Ronin.Server;

/// <summary>A place in a file, as an editor counts them: from zero.</summary>
///
/// <remarks>
///     The compiler counts lines and columns from ONE, because that is what a
///     person reads at the bottom of their screen and what every message it
///     prints has always said. Editors count from zero. The conversion belongs
///     here rather than in either — a compiler that counted from zero would
///     print diagnostics nobody could follow, and a protocol that counted from
///     one would be a protocol nobody else speaks.
/// </remarks>
internal readonly record struct Place(int Line, int Character);

/// <summary>A run of source, from one place to another.</summary>
internal readonly record struct Extent(Place From, Place To);

/// <summary>Something to underline, and what to say about it.</summary>
///
/// <param name="Code">
///     The finding's kind, which is stable and is what an editor dispatches a
///     quick fix on. It is a name rather than a number because a number is a
///     second registry to keep in step with the first.
/// </param>
internal sealed record Reported(Extent Extent, string Message, string Code);

/// <summary>One edit an action applies: text to insert at a place.</summary>
internal readonly record struct Edit(Place At, string Text);

/// <summary>
///     A fix an editor can apply, and the edits that apply it.
/// </summary>
///
/// <param name="Title">
///     What the editor shows in its menu: the statement with this fix's brackets
///     typed in. A person choosing between two bracketings is choosing between
///     two meanings, and the bracketed source is the meaning made visible — where
///     the reading it selects can print the same words as another's and leave the
///     two entries indistinguishable.
/// </param>
internal sealed record Fix(string Title, IReadOnlyList<Edit> Edits);

/// <summary>
///     What an editor asks of the compiler, in the shapes it asks for.
/// </summary>
///
/// <remarks>
///     <para>
///     SEPARATE from the transport, which is the only reason this is a class
///     rather than a message handler. Everything here is a function from source
///     text to an answer, so all of it is testable without a socket, a client,
///     or a running editor — and the part that cannot be tested that way is the
///     part that does nothing but read bytes and write them back.
///     </para>
///     <para>
///     This language needs an editor more than most, and not for comfort: a
///     name is a run of words, so where one starts and stops cannot be seen
///     without knowing what is in scope. Hover answers the question that
///     provokes — «what did the compiler think I wrote» — which is a reading
///     problem, and reading problems are fixed by showing the reader.
///     </para>
/// </remarks>
internal static class Language
{
    /// <summary>Everything wrong with a file, where it is wrong.</summary>
    public static IReadOnlyList<Reported> Diagnostics(SourceText source)
        => [.. Compilation.Of(source)
                          .Findings
                          .Select(finding => new Reported(Where(source, finding.Primary),
                                                          finding.Message,
                                                          finding.Kind.ToString()))];

    /// <summary>
    ///     The fixes for an ambiguity the editor is asking about, if any.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     RECOMPUTED from the text the editor holds, not looked up from the
    ///     diagnostic it sends. A diagnostic is a snapshot; the document may have
    ///     changed since it was published, and a fix built against a stale
    ///     reading would insert brackets where the words no longer are. Resolving
    ///     the current text answers for the current text, and a range that is no
    ///     longer ambiguous simply has no actions.
    ///     </para>
    ///     <para>
    ///     Every reading is a fix, because ambiguity is the error that offers the
    ///     bracketings — the repair search already found which brackets select
    ///     each, and this only turns those into edits an editor can apply. A
    ///     reading whose brackets the search could not find within its budget has
    ///     no action rather than an empty one, the same honesty the search keeps.
    ///     </para>
    /// </remarks>
    public static IReadOnlyList<Fix> Actions(SourceText source, Extent range)
    {
        List<Fix> actions = [];

        foreach (var finding in Compilation.Of(source).Findings.OfType<Ambiguous>())
        {
            if (Overlaps(Where(source, finding.Primary), range) is false) continue;

            foreach (var repair in finding.Repairs)
            {
                actions.Add(new Fix($"Read it as {Previewed(source.Text, finding.Primary, repair.Insertions)}",
                                       [.. repair.Insertions.Select(insertion => new Edit(Place(source, insertion.At), insertion.Text))]));
            }
        }

        return actions;
    }

    /// <summary>
    ///     The ambiguous statement's source with one repair's brackets typed in.
    /// </summary>
    ///
    /// <remarks>
    ///     The title, and its whole job is to tell the menu's entries apart. The
    ///     reading was the title once, and two readings that print alike — «print
    ///     send «a» to «b»» for both «print (send a to b)» and «print (send a) to
    ///     b» — gave two working fixes one label, so a person could not tell which
    ///     meaning either selected. The rendering is a structural identity and was
    ///     never meant to be injective; the brackets are, because a bracketing IS
    ///     the reading it selects. Distinct readings have distinct selecting
    ///     bracketings — a bracketing resolves to one reading or it would not have
    ///     been offered — so the previews differ exactly where the meanings do.
    /// </remarks>
    private static string Previewed(string text, Span span, IReadOnlyList<Insertion> insertions)
    {
        var preview = text.Substring(span.Offset, span.Length);

        // Right to left, so an earlier bracket's index is untouched by a later
        // one's insertion.
        foreach (var insertion in insertions.OrderByDescending(insertion => insertion.At))
        {
            var at = insertion.At - span.Offset;

            preview = preview[..at] + insertion.Text + preview[at..];
        }

        return preview;
    }

    /// <summary>Whether an editor's requested range touches a finding's span.</summary>
    ///
    /// <remarks>
    ///     A code-action request carries the range under the cursor or selection,
    ///     which an editor sets to the diagnostic it is offering a fix for. Any
    ///     overlap counts: a cursor sitting anywhere in an ambiguous statement
    ///     should offer that statement's fixes.
    /// </remarks>
    private static bool Overlaps(Extent finding, Extent range)
        => Before(finding.From, range.To) && Before(range.From, finding.To);

    private static bool Before(Place a, Place b)
        => a.Line < b.Line || (a.Line == b.Line && a.Character <= b.Character);

    /// <summary>
    ///     What the compiler read at a place, with the brackets it inferred.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     The whole statement, because that is the unit with a reading — asking
    ///     about a word would answer about a word, and which words group into
    ///     one name is the entire question. «send «a to b»» and «send «a» to
    ///     «b»» differ nowhere except in the guillemets.
    ///     </para>
    ///     <para>
    ///     Null where there is nothing to say, which is not the same as an empty
    ///     answer: an editor showing an empty box over every space would be
    ///     worse than one that showed nothing. A statement that did not resolve
    ///     has no reading to report and says so by absence, because the
    ///     diagnostic is where that belongs.
    ///     </para>
    /// </remarks>
    public static string Hover(SourceText source, Place at)
    {
        var offset = Offset(source, at);

        foreach (var reading in Compilation.Of(source).Readings)
        {
            if (offset < reading.Span.Offset) continue;
            if (offset >= reading.Span.Offset + reading.Span.Length) continue;
            if (reading.Resolution.Kind is not ResolutionKind.Resolved) continue;

            return reading.Resolution.Reading;
        }

        return null;
    }

    /// <summary>A compiler span as an editor's range.</summary>
    private static Extent Where(SourceText source, Span span)
        => new(Place(source, span.Offset), Place(source, span.Offset + span.Length));

    private static Place Place(SourceText source, int offset)
    {
        var (line, column) = source.At(offset);

        return new Place(line - 1, column - 1);
    }

    /// <summary>
    ///     A place as an offset, which is what a span is measured in.
    /// </summary>
    ///
    /// <remarks>
    ///     Counted rather than indexed. The compiler holds one string and finds
    ///     a line by scanning it, so there is no line table to consult and
    ///     building one here would be a second answer to a question the compiler
    ///     already answers — which is how two of these came to disagree once
    ///     before.
    /// </remarks>
    private static int Offset(SourceText source, Place at)
    {
        var offset = 0;
        var line = 0;

        while (line < at.Line && offset < source.Text.Length)
        {
            if (source.Text[offset] is '\n') ++line;

            ++offset;
        }

        return offset + at.Character;
    }
}
