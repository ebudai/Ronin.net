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

    /// <summary>One pattern's anchor run begins another's.</summary>
    AnchorPrefix,

    /// <summary>A pattern uses a reserved word as a segment.</summary>
    ReservedSegment,

    /// <summary>A multi-word name contains a word that is glue in some pattern.</summary>
    GlueInName,

    /// <summary>The same, for a name the compiler injected.</summary>
    GlueInInjectedName,

    /// <summary>A ring of whens, each writing something the next reads.</summary>
    CascadeRing,

    /// <summary>A cell written by more than one when.</summary>
    ManyWriters,

    /// <summary>A ring of initialisers, each reading the one before it.</summary>
    InitialisationRing,

    /// <summary>Input the grammar could not account for.</summary>
    Malformed,
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
///     findings <em>so far</em> has a repair anyone but the author can choose,
///     since renaming needs a name. Two coming ones do, and either should bring
///     it — inserting the space an unspaced separator wants, and, more valuably,
///     the repair for a tie. The resolver has already enumerated the competing
///     readings by the time it reports one, and bracketing recovers each of them
///     with no authoring judgement at all, so a tie can offer every reading as a
///     one-click alternative.
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
    /// <summary>
    ///     Whether a kind has a message. Asked of every value of the enum by the
    ///     totality test, which is the whole reason this is a lookup rather than
    ///     a switch: a switch needs a default arm, the default arm is a message,
    ///     and a kind added later would then quietly inherit whichever sentence
    ///     happened to be sitting there.
    /// </summary>
    public static bool Renders(FindingKind kind) => Messages.ContainsKey(kind);

    public static string Render(Finding finding) => Messages[finding.Kind](finding);

    private static readonly System.Collections.Generic.Dictionary<FindingKind, System.Func<Finding, string>> Messages = new()
    {
        [FindingKind.Malformed] = finding =>
            $"{finding["reason"]}. «{finding["text"]}» could not be read, and the rest of the " +
            "statement was skipped so that one mistake is reported once.",

        [FindingKind.Shadowed] = finding =>
            $"«{finding["name"]}» is already declared {finding["where"]}. Shadowing is not " +
            "allowed, because reading a value has to tell you where it came from, and the " +
            "compiler cannot flag the ambiguity when both readings are legal. Rename this one.",

        [FindingKind.ReservedPrefix] = finding =>
            $"«{finding["name"]}» begins with the reserved word «{finding["word"]}», which is " +
            "injected rather than declared. Respell it.",

        [FindingKind.Overloaded] = finding =>
            $"«{finding["pattern"]}» has {finding["count"]} declarations and type-directed " +
            "selection is not implemented, so there is no way to choose between them yet. " +
            "Give them different shapes for now.",

        [FindingKind.AnchorPrefix] = finding =>
            $"the anchor of «{finding["prefix"]}» begins that of «{finding["pattern"]}», so a " +
            "statement can read as either and no bracketing tells them apart. Respell one of " +
            "them.",

        [FindingKind.ReservedSegment] = finding =>
            $"«{finding["pattern"]}» uses the reserved word «{finding["word"]}» as a segment, " +
            "which would make it glue and reject every injected name in scope. Respell that " +
            "segment.",

        [FindingKind.GlueInName] = finding =>
            $"«{finding["name"]}» contains «{finding["word"]}», which is glue in " +
            $"«{finding["pattern"]}». A name containing glue silently re-reads statements that " +
            "already worked, so one of the two has to be respelled — and it is the later " +
            "declaration that gives way.",

        [FindingKind.GlueInInjectedName] = finding =>
            $"«{finding["name"]}», injected by «{finding["injector"]}», collides with pattern " +
            $"glue «{finding["word"]}» from «{finding["pattern"]}». Rename " +
            $"«{finding["injector"]}», or respell the pattern.",

        [FindingKind.CascadeRing] = finding =>
            $"«{finding["ring"]}» is a cycle: each writes something the next reads, so firing " +
            "one schedules the next. Stop one of them writing what the ring reads, or declare " +
            "feedback on every when in the ring.",

        [FindingKind.ManyWriters] = finding =>
            $"«{finding["cell"]}» is written by {finding["count"]} whens — «{finding["writers"]}». " +
            "Whens fire in one round with no order between them, so one write would land and the " +
            "other vanish. Derive the value instead, with a let that reads both conditions.",

        [FindingKind.InitialisationRing] = finding =>
            $"«{finding["ring"]}» is a cycle: each initialiser reads the one before it, so none " +
            "of them can be evaluated first. Break the ring by giving one of them a value that " +
            "does not depend on the others.",
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
