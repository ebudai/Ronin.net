// Copyright © 2026 Eric Budai

using System.Collections.Generic;
using System.Globalization;
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

    /// <summary>A name that is exactly a word some pattern uses as glue.</summary>
    GlueAsName,

    /// <summary>The same as GlueInName, for a name the compiler injected.</summary>
    GlueInInjectedName,

    /// <summary>A ring of whens, each writing something the next reads.</summary>
    CascadeRing,

    /// <summary>A cell written by more than one when.</summary>
    ManyWriters,

    /// <summary>A ring of initialisers, each reading the one before it.</summary>
    InitialisationRing,

    /// <summary>Input the grammar could not account for.</summary>
    Malformed,

    /// <summary>A pattern with more words and holes than will be matched.</summary>
    PatternTooWide,

    /// <summary>A pattern that begins with a hole, which is infix and not a word pattern.</summary>
    LeadingHole,
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
internal abstract class Finding(FindingKind kind, Span primary)
{
    public FindingKind Kind { get; } = kind;

    public Span Primary { get; } = primary;

    public IReadOnlyList<Labelled> Related => related;

    /// <summary>The sentence a person reads.</summary>
    public abstract string Message { get; }

    public Finding Alongside(Span span, string label)
    {
        related.Add(new Labelled(span, label));
        return this;
    }

    private readonly List<Labelled> related = [];
}

/// <summary>A name already declared in this scope or an enclosing one.</summary>
internal sealed class Shadowed(Span primary, string name, string where)
    : Finding(FindingKind.Shadowed, primary)
{
    public string Name { get; } = name;
    public string Where { get; } = where;

    public override string Message
        => $"«{Name}» is already declared {Where}. Shadowing is not allowed, because reading a " +
           "value has to tell you where it came from, and the compiler cannot flag the ambiguity " +
           "when both readings are legal. Rename this one.";
}

/// <summary>A name spelled like one the compiler injects.</summary>
internal sealed class ReservedPrefix(Span primary, string name, string word)
    : Finding(FindingKind.ReservedPrefix, primary)
{
    public string Name { get; } = name;
    public string Word { get; } = word;

    public override string Message
        => $"«{Name}» begins with the reserved word «{Word}», which is injected rather than " +
           "declared. Respell it.";
}

/// <summary>More declarations of one shape than can yet be chosen between.</summary>
internal sealed class Overloaded(Span primary, string pattern, int count)
    : Finding(FindingKind.Overloaded, primary)
{
    public string Pattern { get; } = pattern;
    public int Count { get; } = count;

    public override string Message
        => $"«{Pattern}» has {Count.ToString(CultureInfo.InvariantCulture)} declarations and " +
           "type-directed selection is not implemented, so there is no way to choose between them " +
           "yet. Give them different shapes for now.";
}

/// <summary>One pattern's anchor run begins another's.</summary>
internal sealed class AnchorPrefix(Span primary, string pattern, string prefix)
    : Finding(FindingKind.AnchorPrefix, primary)
{
    public string Pattern { get; } = pattern;
    public string Prefix { get; } = prefix;

    public override string Message
        => $"the anchor of «{Prefix}» begins that of «{Pattern}», so a statement can read as " +
           "either and no bracketing tells them apart. Respell one of them.";
}

/// <summary>A pattern uses a reserved word as a segment.</summary>
internal sealed class ReservedSegment(Span primary, string pattern, string word)
    : Finding(FindingKind.ReservedSegment, primary)
{
    public string Pattern { get; } = pattern;
    public string Word { get; } = word;

    public override string Message
        => $"«{Pattern}» uses the reserved word «{Word}» as a segment, which would make it glue " +
           "and reject every injected name in scope. Respell that segment.";
}

/// <summary>A multi-word name contains a word that is glue in some pattern.</summary>
internal sealed class GlueInName(Span primary, string name, string word, string pattern)
    : Finding(FindingKind.GlueInName, primary)
{
    public string Name { get; } = name;
    public string Word { get; } = word;
    public string Pattern { get; } = pattern;

    public override string Message
        => $"«{Name}» contains «{Word}», which is glue in «{Pattern}». A name containing glue " +
           "silently re-reads statements that already worked, so one of the two has to be " +
           "respelled — and it is the later declaration that gives way.";
}

/// <summary>
///     A name the compiler injected that contains pattern glue.
/// </summary>
///
/// <remarks>
///     Removed once, when the only injected name was «old x» — that adds a
///     single word, «old», which <see cref="Rules"/> already refuses as a
///     segment, so an injected name could not offend unless the name it came
///     from did. A loop's «index of bank» adds two words, and either can be
///     glue while «bank» is not, so the shape is back and so is the finding.
///
///     It names the DECLARATION that caused the injection rather than the
///     generated name, because «index of bank» is not the programmer's to
///     rename — the loop variable is.
/// </remarks>
internal sealed class GlueInInjectedName(Span primary, string name, string injector, string word, string pattern)
    : Finding(FindingKind.GlueInInjectedName, primary)
{
    public string Name { get; } = name;
    public string Injector { get; } = injector;
    public string Word { get; } = word;
    public string Pattern { get; } = pattern;

    public override string Message
        => $"«{Name}», injected by «{Injector}», collides with pattern glue «{Word}» from " +
           $"«{Pattern}». Rename «{Injector}», or respell the pattern.";
}

/// <summary>
///     A name that is exactly a word some pattern uses as glue.
/// </summary>
///
/// <remarks>
///     Legibility rather than safety, and it matters which: a single-word name
///     cannot capture anything, because capture needs a multi-word name
///     straddling a hole and that is what <see cref="GlueInName"/> governs. «for
///     each bank in in» resolves uniquely. So this rule is here to stop a reader
///     meeting «in» as a variable in a language where «in» separates a loop
///     header — and being a legibility rule is exactly why it is enforced at the
///     declaration, where the message can name the pattern responsible, rather
///     than in the lexer, where it would be untyped, unscoped and permanent.
/// </remarks>
internal sealed class GlueAsName(Span primary, string name, string pattern)
    : Finding(FindingKind.GlueAsName, primary)
{
    public string Name { get; } = name;
    public string Pattern { get; } = pattern;

    public override string Message
        => $"«{Name}» is the word «{Pattern}» uses to separate its parts, so a reader meets it in " +
           "two roles at once. Rename it — nothing about the program is ambiguous, but a name that " +
           "doubles as punctuation is a name that has to be read twice.";
}

/// <summary>A ring of whens, each writing something the next reads.</summary>
internal sealed class CascadeRing(Span primary, string ring)
    : Finding(FindingKind.CascadeRing, primary)
{
    public string Ring { get; } = ring;

    public override string Message
        => $"«{Ring}» is a cycle: each writes something the next reads, so firing one schedules " +
           "the next. Stop one of them writing what the ring reads, or declare feedback on every " +
           "when in the ring.";
}

/// <summary>A cell written by more than one when.</summary>
internal sealed class ManyWriters(Span primary, string cell, IReadOnlyCollection<string> writers)
    : Finding(FindingKind.ManyWriters, primary)
{
    public string Cell { get; } = cell;
    public IReadOnlyCollection<string> Writers { get; } = [.. writers];

    public override string Message
        => $"«{Cell}» is written by {Writers.Count.ToString(CultureInfo.InvariantCulture)} whens — " +
           $"«{string.Join("» and «", Writers)}». Whens fire in one round with no order between " +
           "them, so one write would land and the other vanish. Derive the value instead, with a " +
           "let that reads both conditions.";
}

/// <summary>A ring of initialisers, each reading the one before it.</summary>
internal sealed class InitialisationRing(Span primary, string ring)
    : Finding(FindingKind.InitialisationRing, primary)
{
    public string Ring { get; } = ring;

    public override string Message
        => $"«{Ring}» is a cycle: each initialiser reads the one before it, so none of them can " +
           "be evaluated first. Break the ring by giving one of them a value that does not depend " +
           "on the others.";
}

/// <summary>
///     A pattern that begins with a hole.
/// </summary>
///
/// <remarks>
///     Its own rule and its own message, checked before the anchor comparison.
///     A leading hole makes the anchor run empty, and an empty run is a prefix
///     of every other — so R6 rejects infix already, but by accident, and would
///     say one anchor run begins another when the actual problem is that there
///     is no anchor run at all.
/// </remarks>
internal sealed class LeadingHole(Span primary, string pattern)
    : Finding(FindingKind.LeadingHole, primary)
{
    public string Pattern { get; } = pattern;

    public override string Message
        => $"«{Pattern}» begins with a parameter, which makes it infix rather than a word " +
           "pattern. A word pattern leads with its name — respell it so the words come first, " +
           "or declare a symbolic operator, which is where infix belongs.";
}

/// <summary>A pattern with more words and holes than will be matched.</summary>
internal sealed class PatternTooWide(Span primary, string name, int width, int most)
    : Finding(FindingKind.PatternTooWide, primary)
{
    public string Name { get; } = name;
    public int Width { get; } = width;
    public int Most { get; } = most;

    /// <remarks>
    ///     Elided, because a name wide enough to trip this is by definition too
    ///     wide to print — and the same helper a cycle ring uses for the same
    ///     reason keeps both ends, which are the informative ones.
    /// </remarks>
    public override string Message
        => $"«{Triggers.Elide(Name)}» has {Width.ToString(CultureInfo.InvariantCulture)} words and holes, and a " +
           $"pattern may have at most {Most.ToString(CultureInfo.InvariantCulture)}. Matching one " +
           "walks a frame per segment, so the limit is what keeps a declaration from being a way " +
           "to exhaust the stack. Split it into smaller patterns.";
}

/// <summary>Input the grammar could not account for.</summary>
internal sealed class Malformed(Span primary, string reason, string text)
    : Finding(FindingKind.Malformed, primary)
{
    public string Reason { get; } = reason;
    public string Text { get; } = text;

    public override string Message
        => $"{Reason}. «{Text}» could not be read, and the rest of the statement was skipped so " +
           "that one mistake is reported once.";
}

/// <summary>
///     Turns a finding into what a build prints.
/// </summary>
///
/// <remarks>
///     The wording lives on the findings themselves now, beside the roles each
///     one needs. It used to be a lookup from kind to a lambda indexing a
///     dictionary of strings by role — so a producer that spelled a role
///     differently, or forgot one, produced a KeyNotFoundException at report
///     time, and the totality test could only prove that every kind had ONE
///     producer supplying them correctly. A kind's roles are its constructor
///     parameters now, so a finding that cannot be rendered cannot be built.
/// </remarks>
internal static class Diagnostics
{
    public static string Render(Finding finding) => finding.Message;

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
