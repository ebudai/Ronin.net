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
