// Copyright © 2026 Eric Budai

using System;
using System.Collections.Generic;

namespace Ronin.Compiler;

/// <summary>
///     A stretch of one source text.
/// </summary>
///
/// <remarks>
///     <para>
///     Offsets are only comparable within a source, so the source is part of the
///     span rather than context around it. A diagnostic naming two files is
///     already known to be coming — a pattern exported by one module colliding
///     with a name exported by another, where neither declaration is in the file
///     being compiled.
///     </para>
///     <para>
///     Zero length is legal and means a position rather than a range: "expected a
///     type after «=&gt;»" points between two tokens, and forcing it to cover one
///     would make it point at the wrong character.
///     </para>
///     <para>
///     Offsets are UTF-16 code units, because <c>Token.Memory</c> is
///     <c>ReadOnlyMemory&lt;char&gt;</c> and <c>RunningIndex</c> advances by its
///     length. That is also what the language server protocol wants, so nothing
///     needs converting — and writing it down is what stops something later
///     converting to bytes or codepoints, which stays invisible until someone
///     puts an emoji in a text literal.
///     </para>
/// </remarks>
internal readonly record struct Span(SourceText Source, int Offset, int Length)
{
    /// <summary>Where a caret goes: the position, with nothing under it.</summary>
    public Span At => this with { Length = 0 };

    public override string ToString() => Source.Describe(this);
}

/// <summary>
///     One text that spans point into.
/// </summary>
///
/// <remarks>
///     A path is optional because a buffer in an editor has none and a test has
///     none, and neither needs one to be correct. Identity is the object itself.
/// </remarks>
internal sealed class SourceText(string text, string path = null)
{
    public string Text { get; } = text ?? throw new ArgumentNullException(nameof(text));

    public string Path { get; } = path;

    /// <summary>
    ///     A stretch of this text.
    /// </summary>
    ///
    /// <remarks>
    ///     Bounds checked here rather than where a line number is asked for. A
    ///     span built from an offset that is not in the text is a defect in
    ///     whatever computed it — a token offset taken from the wrong source, a
    ///     length measured in bytes — and it produces a plausible-looking
    ///     location instead of a failure, which is the hardest kind to trace back.
    ///     An offset AT the end is legal and common: it is where «expected a type
    ///     after «=&gt;»» points when the file simply stops.
    /// </remarks>
    public Span Span(int offset, int length)
    {
        if (offset < 0 || offset > Text.Length)
            throw new ArgumentOutOfRangeException(nameof(offset), offset,
                                                  $"outside a text of {Text.Length} characters");

        // «Text.Length - offset» and not «offset + length», which wraps
        // negative for a large enough length and lets an invalid span through
        // the check meant to stop exactly that. Offset is already validated, so
        // the subtraction cannot wrap.
        if (length < 0 || length > Text.Length - offset)
            throw new ArgumentOutOfRangeException(nameof(length), length,
                                                  $"reaches past the end of a text of {Text.Length} characters");

        return new(this, offset, length);
    }

    /// <summary>
    ///     The one-based line and column of an offset. The line table is built on
    ///     first use and kept, because most texts are never asked.
    /// </summary>
    public (int Line, int Column) At(int offset)
    {
        starts ??= Starts(Text);

        // the last line starting at or before the offset
        var low = 0;
        var high = starts.Count - 1;

        while (low < high)
        {
            var middle = (low + high + 1) / 2;
            if (starts[middle] <= offset) low = middle; else high = middle - 1;
        }

        return (low + 1, offset - starts[low] + 1);
    }

    public string Describe(Span span)
    {
        var (line, column) = At(span.Offset);
        return $"{Path ?? "source"}:{line}:{column}";
    }

    private static List<int> Starts(string text)
    {
        List<int> starts = [0];

        for (var i = 0; i != text.Length; ++i)
        {
            if (text[i] is '\n') starts.Add(i + 1);
        }

        return starts;
    }

    private List<int> starts;
}
