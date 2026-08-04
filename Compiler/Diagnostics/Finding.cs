// Copyright © 2026 Eric Budai

using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    /// <summary>A pattern uses as glue a word the compiler injects names with.</summary>
    InjectionWordAsGlue,

    /// <summary>A multi-word name contains a word that is glue in some pattern.</summary>
    GlueInName,

    /// <summary>A name that is exactly a word some pattern uses as glue.</summary>
    GlueAsName,

    /// <summary>A name containing a word the language reads as an operator.</summary>
    InfixInName,

    /// <summary>A pattern using a word the language reads as an operator.</summary>
    InfixInPattern,

    /// <summary>A name that would swallow a pattern's whole call.</summary>
    NameShadowsPattern,

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

    /// <summary>A declaration whose words do not read back as themselves.</summary>
    UnwritableName,

    /// <summary>A bracket in a declaration with no name inside it.</summary>
    EmptyHole,

    /// <summary>A hole where a plain name is required.</summary>
    HoleInName,

    /// <summary>A «when» declared where it could never run.</summary>
    MisplacedWhen,

    /// <summary>A «when» inside a type, which is designed and not joined to instances yet.</summary>
    WhenInType,

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

    /// <remarks>
    ///     The same wrapping as everywhere else: labels could be removed or
    ///     reordered through the read-only type before anything rendered them.
    /// </remarks>
    public IReadOnlyList<Labelled> Related => view ??= new ReadOnlyCollection<Labelled>(related);

    /// <summary>The sentence a person reads.</summary>
    public abstract string Message { get; }

    public Finding Alongside(Span span, string label)
    {
        related.Add(new Labelled(span, label));
        return this;
    }

    private readonly List<Labelled> related = [];

    /// <remarks>
    ///     ONE view over the live list, and not one per read. «AsReadOnly» built
    ///     a fresh wrapper every time — 24 bytes a read, for an object that
    ///     never needs to change, since a view already shows what «Alongside»
    ///     adds. The graph and the compilation cache theirs; this did not.
    ///
    ///     Built on first ask rather than in a constructor, because the primary
    ///     constructor has no body to build it in and a field initialiser cannot
    ///     see «related». A finding that nobody renders never builds one.
    /// </remarks>
    private ReadOnlyCollection<Labelled> view;
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

/// <summary>
///     A name beginning with every word of a pattern, which swallows its call.
/// </summary>
///
/// <remarks>
///     R6 compares patterns with patterns. This is the pattern-versus-NAME case,
///     and it is the same arithmetic as <see cref="InfixInName"/>: a name is one
///     lookup and a call is one plus its arguments, so a name covering the call's
///     whole span is always cheaper and always wins, without a tie to report.
///     <para>
///     Only a pattern with no glue can be caught this way. One with glue needs
///     that word inside the name to reach the whole call, and R5 has already
///     refused it — so this asks only the patterns R5 leaves exposed, which is
///     also why it is the anchor-only shapes that the registry has to warn about.
///     </para>
/// </remarks>
internal sealed class NameShadowsPattern(Span primary, string name, string pattern)
    : Finding(FindingKind.NameShadowsPattern, primary)
{
    public string Name { get; } = name;
    public string Pattern { get; } = pattern;

    public override string Message
        => $"«{Name}» begins with every word of «{Pattern}», so it would be read instead of that " +
           "call wherever both are in scope — and more cheaply, so nothing would report it. " +
           "Rename it, or respell the pattern.";
}

/// <summary>
///     A pattern using a word the language reads as an operator.
/// </summary>
///
/// <remarks>
///     The other half of <see cref="InfixInName"/>, and it fails differently: a
///     name is cheaper than the expression it covers and wins silently, while a
///     pattern costs exactly what the operator costs and TIES. So the name case
///     is a silent capture and this one is an ambiguity at every call site —
///     which is a worse diagnostic for the same defect, because it is reported
///     far from the declaration that caused it and once per use.
/// </remarks>
internal sealed class InfixInPattern(Span primary, string pattern, string word)
    : Finding(FindingKind.InfixInPattern, primary)
{
    public string Pattern { get; } = pattern;
    public string Word { get; } = word;

    public override string Message
        => $"«{Pattern}» uses «{Word}», which the language reads as an operator between two " +
           "values. A call to it would cost exactly what the operation costs, so every " +
           $"«… {Word} …» in scope would be ambiguous rather than wrong. Respell it.";
}

/// <summary>
///     A name containing a word the language reads as an operator.
/// </summary>
///
/// <remarks>
///     R5's shape, for the same reason and against a different rival. A name is
///     ONE lookup and any composite reading of the same span is at least two, so
///     a name spanning an operator always wins it — silently, because it is
///     cheaper rather than equal. Declaring «x otherwise y» takes every «x
///     otherwise y» already written and makes it mean the name.
/// </remarks>
internal sealed class InfixInName(Span primary, string name, string word)
    : Finding(FindingKind.InfixInName, primary)
{
    public string Name { get; } = name;
    public string Word { get; } = word;

    public override string Message
        => $"«{Name}» contains «{Word}», which the language reads as an operator between two " +
           "values. A name spanning one is cheaper than the expression it covers, so every " +
           $"«… {Word} …» already written would quietly become this name instead. Respell it.";
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

/// <summary>
///     A pattern using as glue one of the words the compiler builds injected
///     names from.
/// </summary>
///
/// <remarks>
///     The dual of the reserved list: glue words may not be names, so injection
///     words may not be glue. «index of bank» is a name the compiler writes, and
///     a pattern that makes «of» glue makes that name illegal in every scope it
///     reaches — turning a loop nobody touched into an error.
///
///     Caught at the PATTERN, which fires once, rather than at each injected
///     name, which fires once per loop and names the wrong thing.
/// </remarks>
internal sealed class InjectionWordAsGlue(Span primary, string pattern, string word, string injects)
    : Finding(FindingKind.InjectionWordAsGlue, primary)
{
    public string Pattern { get; } = pattern;
    public string Word { get; } = word;
    public string Injects { get; } = injects;

    public override string Message
        => $"«{Pattern}» may not use «{Word}» as glue: «{Word}» is how the compiler builds the " +
           $"injected name «{Injects}». A pattern that reserves it makes that name illegal " +
           "everywhere this pattern is in scope. Respell the pattern.";
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

/// <summary>
///     A «when» inside a type, which is designed and not implemented.
/// </summary>
///
/// <remarks>
///     Its own kind rather than the general parse failure, because a user who
///     writes this has understood the design and is being told they made a
///     syntax error. For a language whose diagnostics are the teaching
///     mechanism, "designed and not built" and "I cannot read this" must not
///     look the same.
///     <para>
///     What blocks it is no longer the instance binding model. That model is
///     decided AND built — one cell per declared member, a stable
///     <see cref="Runtime.Instance"/> handle rather than an index, creation,
///     removal and member writes. What is missing is the JOIN: nothing turns a
///     «when» in a type body into the one type-scope node that evaluates its
///     predicate across the member array and fires per instance, and no liveness
///     mask exists for «stop» to clear a bit in.
///     </para>
///     <para>
///     Naming the actual remainder matters more here than in most messages,
///     because the previous one told a user to wait for something that had
///     already arrived. So the construct is recognised and refused, which adds a
///     message and no semantics.
///     </para>
/// </remarks>
internal sealed class WhenInType(Span primary) : Finding(FindingKind.WhenInType, primary)
{
    public override string Message
        => "a «when» inside a type is not implemented yet. Instances are built — one cell per member, and a handle " +
           "that survives removal — but nothing yet fires a type-scope «when» per instance, so declare it at module " +
           "scope, or track the instance explicitly.";
}

/// <summary>
///     A «when» declared somewhere it could never run.
/// </summary>
///
/// <remarks>
///     A «when» belongs at module scope or inside a type, and nowhere else. A
///     propagation step happens BETWEEN statements rather than during one, so a
///     «when» declared inside a function body has two possible lifetimes and
///     both are wrong: it leaves its scope before any step runs, in which case
///     it can never fire and the declaration is dead; or it outlives its scope,
///     in which case it holds references to locals that are gone.
///     <para>
///     There is no third option, so the restriction costs nothing — and it is
///     what lets the lifetime rule be stated whole: a module «when» lives as
///     long as the module, a type «when» as long as the instance.
///     </para>
/// </remarks>
internal sealed class MisplacedWhen(Span primary, string inside) : Finding(FindingKind.MisplacedWhen, primary)
{
    /// <summary>What it was declared inside, named the way a reader would.</summary>
    public string Inside { get; } = inside;

    public override string Message
        => $"«when» may only be declared at module scope or inside a type. This one is inside {Inside}, where it " +
           "would go out of scope before it could ever run — a step happens between statements, not during one.";
}

/// <summary>
///     A hole somewhere only a name can go.
/// </summary>
///
/// <remarks>
///     A parameter and a loop variable are NAMES. They are bound to one value
///     on entry, and there is nothing for a hole in one to mean — «function
///     outer (callback (x =&gt; Number) =&gt; Number)» would be a parameter that
///     is itself a pattern, which is a language feature nobody has asked for.
///     <para>
///     Refused rather than flattened. A parameter's identifier was rendered
///     straight to a runtime name by <c>Identifier.Words</c>, which drops every
///     parameter block — so that declaration became the parameter «callback»,
///     «(x =&gt; Number) rounded» became «rounded», and the brackets and the
///     nested declaration went with them, silently.
///     </para>
/// </remarks>
internal sealed class HoleInName(Span primary, string shape) : Finding(FindingKind.HoleInName, primary)
{
    public string Shape { get; } = shape;

    public override string Message
        => $"«{Shape}» has a bracket in it, and this position takes a name. A bracket marks an argument, and a " +
           "parameter is bound to one value rather than taking any — so there is nothing for a hole here to mean. " +
           "Name it, or declare the pattern separately.";
}

/// <summary>
///     A bracket in a declaration with nothing inside it.
/// </summary>
///
/// <remarks>
///     <para>
///     A bracket in a declaration marks A HOLE, not a parameter list — «send
///     (message) to (recipient)» is called «send x to y», so «(message)» is one
///     hole with one name. «()» is therefore a hole with no name: zero-width,
///     referring to nothing. Ronin has no parameter lists for it to be an empty
///     one of.
///     </para>
///     <para>
///     Not accepted as a second spelling of «function ping», which is what
///     already declares a function taking nothing. Someone writing «function
///     ping ()» is importing a habit whose next move is «ping()» at the call
///     site, and that cannot work — «ping» is a plain name and is called «ping».
///     Accepting the declaration buys a moment of familiarity and sets up a
///     worse surprise straight after; refusing it puts the correction where the
///     author still has the model in mind. It would also establish that empty
///     brackets are erasable, which invites «send () to ()» and a rule nobody
///     wants to write.
///     </para>
///     <para>
///     The message explains the model rather than reporting a syntax error,
///     because the mistake is a wrong model and not a typo. It does not offer
///     «(_)» as the unnamed-hole spelling: that is pattern NOTATION, which the
///     registry renders and this compiler's own <c>Pattern.Parse</c> reads, and
///     it is not source. A declaration always names its holes.
///     </para>
/// </remarks>
internal sealed class EmptyHole(Span primary, string shape) : Finding(FindingKind.EmptyHole, primary)
{
    public string Shape { get; } = shape;

    public override string Message
        => $"«{Shape}» has a bracket with nothing in it. In a NAME, a bracket marks one argument — «send (message) " +
           "to (recipient)» is called «send x to y» — so «()» is an argument with no name rather than an empty list " +
           "of them. A function that takes nothing is declared without the brackets. (A delegate is different: its " +
           "brackets are a signature, and «() => …» is a delegate of no arguments.)";
}

/// <summary>
///     A declaration whose words do not read back as the words declared.
/// </summary>
///
/// <remarks>
///     Reachable one way: something that is not whitespace between the two words
///     of a composite keyword. «compute part /* gap */ of (x)» declares the
///     THREE words «compute» «part» «of», because trivia stops «part of» being
///     recognised as the one token it usually is — and written down, those three
///     read back as two.
///     <para>
///     Every declaration and not only a pattern, because the symbol table is
///     keyed on the rendering: a name whose words the rendering cannot state is
///     a name the table cannot tell apart from a different one.
///     </para>
/// </remarks>
internal sealed class UnwritableName(Span primary, string declares, string reads)
    : Finding(FindingKind.UnwritableName, primary)
{
    public string Declares { get; } = declares;
    public string Reads { get; } = reads;

    public override string Message
        => $"this declares the words {Declares}, and written down they read back as {Reads} — a different " +
           "declaration that spells the same. Two words of a composite keyword have something other than a space " +
           "between them; close the gap, or respell it.";
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
