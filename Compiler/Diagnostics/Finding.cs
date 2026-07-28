// Copyright © 2026 Eric Budai

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ronin.Compiler;

/// <summary>
///     What kind of thing was found. Stable identity for a test to assert and for
///     an editor to dispatch a quick fix on.
/// </summary>
internal enum FindingKind
{
    /// <summary>A name already declared in this scope or an enclosing one.</summary>
    Shadowed,

    /// <summary>A name spelled like one the compiler injects.</summary>
    ReservedPrefix,

    /// <summary>More declarations of one shape than can yet be chosen between.</summary>
    Overloaded,
}

/// <summary>A span with a word about why it is being pointed at.</summary>
internal readonly record struct Labelled(Span Span, string Label);

/// <summary>
///     Something the compiler found, before anyone decides how to say it.
/// </summary>
///
/// <remarks>
///     <para>
///     Every diagnostic in this language names two or more things — a tie names
///     both readings, a glue collision names the name and the pattern, a cascade
///     names the whole ring — so one span was never going to be enough. The
///     primary is where the caret goes and the related spans are the rest of the
///     explanation.
///     </para>
///     <para>
///     Symbols are carried as references rather than interpolated into a
///     sentence, so the renderer quotes them the same way everywhere and an
///     editor can make them clickable. Interpolating forecloses both.
///     </para>
///     <para>
///     A symbol the compiler generated has no text of its own — «old smoothed»
///     was never written — so it carries the span of the declaration that caused
///     it, and the message explains the indirection. That is the alternative to a
///     null span, which would otherwise become a special case in the renderer,
///     the editor, and everything else downstream.
///     </para>
///     <para>
///     A machine-applicable fix belongs here too and is not here yet: none of the
///     findings so far has a repair anyone but the author can choose, since
///     renaming needs a name. The first one that does — inserting the space an
///     unspaced separator wants, say — is what should bring it.
///     </para>
/// </remarks>
internal sealed class Finding(FindingKind kind, Span primary)
{
    public FindingKind Kind { get; } = kind;

    public Span Primary { get; } = primary;

    /// <summary>Named by role, so the renderer can say which is which.</summary>
    public Dictionary<string, string> Symbols { get; } = [];

    public List<Labelled> Related { get; } = [];

    public Finding Naming(string role, string symbol)
    {
        Symbols[role] = symbol;
        return this;
    }

    public Finding Alongside(Span span, string label)
    {
        Related.Add(new Labelled(span, label));
        return this;
    }

    public string this[string role] => Symbols[role];
}

/// <summary>
///     Turns a finding into the sentence a person reads.
/// </summary>
///
/// <remarks>
///     Separate from the rules so that wording stays cheap to improve. Every
///     message here has been revised at least once — what to bracket, whether an
///     overflow is a mistake or a limit, which of two names to rename — and each
///     revision used to break the test of a rule that had not changed.
/// </remarks>
internal static class Diagnostics
{
    public static string Render(Finding finding) => finding.Kind switch
    {
        FindingKind.Shadowed =>
            $"«{finding["name"]}» is already declared {finding["where"]}. Shadowing is not " +
            "allowed, because reading a value has to tell you where it came from, and the " +
            "compiler cannot flag the ambiguity when both readings are legal. Rename this one.",

        FindingKind.ReservedPrefix =>
            $"«{finding["name"]}» begins with the reserved word «{finding["word"]}», which is " +
            "injected rather than declared. Respell it.",

        _ => $"«{finding["pattern"]}» has {finding["count"]} declarations and type-directed " +
             "selection is not implemented, so there is no way to choose between them yet. " +
             "Give them different shapes for now.",
    };

    /// <summary>The sentence with its spans, as a build would print it.</summary>
    public static string Report(Finding finding)
    {
        StringBuilder report = new();

        report.Append($"{finding.Primary}: {Render(finding)}");

        foreach (var related in finding.Related)
        {
            report.Append($"{System.Environment.NewLine}    {related.Span}: {related.Label}");
        }

        return report.ToString();
    }
}
