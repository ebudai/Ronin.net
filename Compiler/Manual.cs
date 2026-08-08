// Copyright © 2026 Eric Budai

using System;
using System.Linq;
using System.Text;

namespace Ronin.Compiler;

/// <summary>
///     The language reference, written out from what the language supplies.
/// </summary>
///
/// <remarks>
///     <para>
///     GENERATED, and that is the whole point. A hand-maintained page per builtin
///     drifts from the builtin and nothing says when — four documented instances
///     on this project, one of them a fact answered in conversation and never
///     written down at all. A description that lives on the declaration cannot
///     drift from it, and the failure mode stops depending on somebody
///     remembering.
///     </para>
///     <para>
///     ONE FILE. Per-entry pages are a rendering choice and reversible whenever
///     published docs want URLs, and the reversibility is free — where a file per
///     entry costs a golden per entry, and a forgotten one is the silent gap this
///     exists to close. One diff also puts an edit to «return» next to «return
///     (_)», which is where it wants reading, since the two are written to point
///     at each other.
///     </para>
///     <para>
///     Sibling of <see cref="Glue"/>'s registry rather than a new mechanism: the
///     same table, a second rendering, and the same discipline of a committed
///     file a test compares against.
///     </para>
/// </remarks>
internal static class Manual
{
    public static string Of(System.Collections.Generic.IReadOnlyList<Descriptor> supplies)
    {
        ArgumentNullException.ThrowIfNull(supplies);

        StringBuilder reference = new();

        reference.AppendLine("# Reference");
        reference.AppendLine();
        reference.AppendLine("GENERATED from the language's own table — every entry below is the description");
        reference.AppendLine("carried by the thing it describes. Editing this file does nothing: the next build");
        reference.AppendLine("writes it again. Change the summary where the entry is declared.");
        reference.AppendLine();
        reference.AppendLine("The guide is the hand-written half, and answers «how do I do X». This answers");
        reference.AppendLine("«what exactly is Y».");

        foreach (var supplied in supplies.OrderBy(supplied => supplied.Name, StringComparer.Ordinal))
        {
            reference.AppendLine();
            reference.AppendLine($"## {supplied.Name}");
            reference.AppendLine();
            reference.AppendLine(supplied.Summary);

            foreach (var form in supplied.Forms)
            {
                reference.AppendLine();
                reference.AppendLine($"    {form}");
            }

            if (supplied.Legal is not null)
            {
                reference.AppendLine();
                reference.AppendLine(supplied.Legal);
            }

            if (supplied.SeeAlso.Count is 0) continue;

            reference.AppendLine();
            reference.AppendLine($"See also: {string.Join(", ", supplied.SeeAlso.Select(name => $"«{name}»"))}.");
        }

        return reference.ToString();
    }
}
