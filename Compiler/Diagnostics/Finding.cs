// Copyright © 2026 Eric Budai

using System;
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

    /// <summary>More declarations of one shape than can yet be chosen between.</summary>
    Overloaded,

    /// <summary>One shape declared twice with the same parameter types.</summary>
    DuplicateSignature,

    /// <summary>A name or shape already supplied by the language.</summary>
    Supplied,

    /// <summary>One pattern's anchor run begins another's.</summary>
    AnchorPrefix,

    /// <summary>A pattern uses as glue a word the compiler injects names with.</summary>
    InjectionWordAsGlue,




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

    /// <summary>A name containing a word the language reads as an operator.</summary>
    InfixInName,

    /// <summary>A statement whose words have more than one reading.</summary>
    Ambiguous,

    /// <summary>A «when» body answering, when nothing consumes a reaction's value.</summary>
    AnsweringReaction,

    /// <summary>A body that both answers and does not.</summary>
    MixedExits,

    /// <summary>«stop» where there is no «when» to remove.</summary>
    MisplacedStop,

    /// <summary>A type annotation whose words name no type.</summary>
    UnknownType,

    /// <summary>A type annotation with more words and symbols than are read at once.</summary>
    OversizeType,

    /// <summary>A datum or datatype named by a pattern, which only a function may be.</summary>
    Parameterized,

    /// <summary>A value whose sort is not the type its declaration names.</summary>
    TypeMismatch,

    /// <summary>Two of a function's returns give different types, with no written return type to choose between them.</summary>
    DivergentReturns,

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
///     A symbol the compiler generated has no text of its own — «index of bank»
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


/// <summary>A name whose complete span also reads as a pattern call.</summary>
///
/// <remarks>
///     R6 compares patterns with patterns. This is the pattern-versus-NAME case,
///     and it is the same arithmetic as <see cref="InfixInName"/>: a name is one
///     lookup and a call is one plus its arguments, so a name covering the call's
///     whole span is always cheaper and always wins, without a tie to report.
///     <para>
///     The whole span is load-bearing. An anchor-only pattern collides with any
///     longer name beginning with its anchor, while a glued or pinned pattern
///     collides only with a complete name that conforms to its shape. «a to b»
///     is therefore legal beside «send (_) to (_)» and «send x to y» is not.
///     </para>
/// </remarks>
internal sealed class NameShadowsPattern(Span primary, string name, string pattern, string injectedBy = null,
                                         bool universal = false, bool builtin = false)
    : Finding(FindingKind.NameShadowsPattern, primary)
{
    public string Name { get; } = name;
    public string Pattern { get; } = pattern;

    /// <summary>The declaration the compiler built this name from, where it built it.</summary>
    public string InjectedBy { get; } = injectedBy;

    /// <summary>Whether every name that injection could build collides, not only this one.</summary>
    public bool Universal { get; } = universal;

    /// <summary>Whether the rival is supplied by the language and cannot be respelled.</summary>
    public bool Builtin { get; } = builtin;

    /// <remarks>
    ///     <para>
    ///     A BUILT name gets its own sentence, because the repair is not the
    ///     same, and which sentence depends on how much of the pattern lands in
    ///     the words the compiler chose. All of it, and no loop variable in any
    ///     program avoids the collision — so the pattern is the only party and
    ///     one message covers every loop in scope, rather than one message per
    ///     loop naming a different counter and asking for the same edit.
    ///     </para>
    ///     <para>
    ///     Past it, and the subject is load-bearing: «index of bank (_)» collides
    ///     only with counters for variables starting «bank», so renaming the
    ///     variable works as well as respelling the pattern. Both parties are
    ///     actionable, so the standing convention applies and the later
    ///     declaration gives way — the same rule as any two written names, which
    ///     it stopped being when a built name was treated as unchangeable
    ///     whichever half of it collided.
    ///     </para>
    /// </remarks>
    public override string Message
        => Universal
         ? $"«{Pattern}» cannot be declared: the compiler builds «{Name}» wherever one is needed, and " +
           "that name's complete span also reads as a call to the pattern. No bracketing selects the name " +
           "reading. Respell the pattern; the collision is in the compiler's own words, so no name in " +
           "the source avoids this."
         : Builtin
         ? $"«{Name}» cannot be a name: its complete span also reads as a call to the built-in " +
           $"«{Pattern}», and no bracketing selects the name reading. Rename it; a built-in cannot be respelled."
         : InjectedBy is null
         ? $"«{Name}» cannot be a name: its complete span also reads as a call to «{Pattern}», and no " +
           "bracketing selects the name reading. Rename it, or respell the pattern."
         : $"«{Name}» has another reading over its complete span: a call to «{Pattern}», with no " +
           $"bracketing that selects the name. The compiler builds it from «{InjectedBy}»: rename that, " +
           "or respell the pattern.";
}

/// <summary>
///     A pattern using a word the language reads as an operator.
/// </summary>
///
/// <remarks>
///     The other half of <see cref="InfixInName"/>, refused for the same reason
///     from the other side: a call to this pattern and the operator expression
///     cover the same span, and no bracketing tells them apart. «(x) otherwise
///     (y)» is still both.
///     <para>
///     Refused at the DECLARATION because that is the one place it can be said
///     once. An unrepairable ambiguity is otherwise reported at every call site,
///     none of which is where the mistake was made.
///     </para>
/// </remarks>
internal sealed class Ambiguous(Span primary,
                               IReadOnlyList<string> readings,
                               IReadOnlyList<Repair> repairs,
                               long total,
                               bool bounded)
    : Finding(FindingKind.Ambiguous, primary)
{
    /// <summary>
    ///     Each reading and the edit that selects it, cheapest first.
    /// </summary>
    ///
    /// <remarks>
    ///     The repair is data rather than prose because a message cannot be
    ///     clicked. Every reading here is reachable by one bracket pair — which
    ///     is the property the whole direction rests on and is searched for
    ///     rather than assumed — so this is an error that carries its own
    ///     answers.
    /// </remarks>
    public IReadOnlyList<Repair> Repairs { get; } = Owned.Copy(repairs);

    /// <summary>
    ///     The cheapest readings, in order, which is not always all of them.
    /// </summary>
    ///
    /// <remarks>
    ///     Every reading, and not only the ones a repair was found for. Deriving
    ///     these FROM the repairs made a statement whose repairs need two
    ///     brackets report that it reads no ways at all — a count of two above
    ///     an empty list, which is worse than either half alone.
    /// </remarks>
    public IReadOnlyList<string> Readings { get; } = Owned.Copy(readings);

    /// <summary>How many readings there are.</summary>
    public long Total { get; } = total;

    /// <summary>Whether <see cref="Total"/> is a floor rather than a count.</summary>
    public bool Bounded { get; } = bounded;

    public override string Message
        => $"this reads {Count} ways and the compiler will not choose between them:{Shown}Bracket the one you meant.";

    private string Count
        => Bounded
         ? "at least " + Total.ToString(CultureInfo.InvariantCulture)
         : Total.ToString(CultureInfo.InvariantCulture);

    private string Shown
        => Readings.Aggregate(new StringBuilder(),
                              (shown, reading) => shown.Append(Environment.NewLine).Append("    ").Append(reading),
                              shown => shown.Append(Environment.NewLine).Append(Environment.NewLine).ToString());
}

/// <summary>
///     A «when» body carrying a value out of itself.
/// </summary>
///
/// <remarks>
///     «return (_)» and bare «return» are one concept at two arities — leave this
///     body now, with or without an answer. A reaction has nobody to answer, so
///     only the second is legal in one, and the message says which of the two
///     neighbouring words is wanted rather than leaving the reader to guess.
///     <para>
///     REMOVE rather than disarm, which is the word the design offered and the
///     one thing in its sentence that is not what happens. «Graph.Stop» says it
///     itself — "it REMOVES the node rather than disabling it" — and disarm reads
///     as reversible when nothing can re-arm a «when». The pair this message
///     exists to separate is not helped by describing one of them loosely.
///     </para>
/// </remarks>
internal sealed class AnsweringReaction(Span primary)
    : Finding(FindingKind.AnsweringReaction, primary)
{
    public override string Message
        => "«return (_)» in a «when» body — a reaction has nobody to answer. Use «return» to end this " +
           "firing and leave the «when» in place, or «stop» to remove it.";
}

/// <summary>
///     A body that both answers and does not.
/// </summary>
///
/// <remarks>
///     A body has one exit flavour, decided by whether any «return (_)» appears
///     in it — and this is not a rule of its own. It is the check that stops the
///     return type having two answers, seen from the other side: a body that
///     sometimes carries a value and sometimes does not has no one type to infer.
/// </remarks>
internal sealed class MisplacedStop(Span primary)
    : Finding(FindingKind.MisplacedStop, primary)
{
    public override string Message
        => "«stop» removes the «when» it is written in, and there is none here. To leave this body, " +
           "write «return».";
}

/// <summary>
///     A body that both answers and does not.
/// </summary>
///
/// <remarks>
///     A body has one exit flavour, decided by whether any «return (_)» appears
///     in it — and this is not a rule of its own. It is the check that stops the
///     return type having two answers, seen from the other side: a body that
///     sometimes carries a value and sometimes does not has no one type to infer.
/// </remarks>
internal sealed class MixedExits(Span primary)
    : Finding(FindingKind.MixedExits, primary)
{
    public override string Message
        => "this body both answers and leaves without answering. A body does one or the other — give " +
           "every «return» a value, or none of them.";
}

/// <summary>A pattern using a word the language reads as an operator.</summary>
///
/// <remarks>
///     The same reason from the other side. A call to this pattern and the
///     operator expression cover the same span, and no bracketing tells them
///     apart — «(x) otherwise (y)» is still both.
///     <para>
///     Refused at the DECLARATION because that is the one place it can be said
///     once. An unrepairable ambiguity is otherwise reported at every call site,
///     none of which is where the mistake was made.
///     </para>
/// </remarks>
internal sealed class InfixInPattern(Span primary, string pattern, string word)
    : Finding(FindingKind.InfixInPattern, primary)
{
    public string Pattern { get; } = pattern;
    public string Word { get; } = word;

    public override string Message
        => $"«{Pattern}» uses «{Word}», which the language reads as an operator between two " +
           $"values. A call to it covers the same span as the operation, so every «… {Word} …» in " +
           "scope would have both readings and no bracketing would tell them apart. Respell it.";
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
internal sealed class InfixInName(Span primary, string name, string word, bool built = false)
    : Finding(FindingKind.InfixInName, primary)
{
    /// <summary>The name to rename, which is not always the name that offends.</summary>
    public string Name { get; } = name;

    public string Word { get; } = word;

    /// <summary>Whether what offends is built from <see cref="Name"/> rather than being it.</summary>
    public bool Built { get; } = built;

    /// <remarks>
    ///     A BUILT name asks for the same edit somewhere else. Nobody wrote
    ///     «index of is valid» and nobody can respell the operator it spans, so
    ///     the subject the compiler copied is the only actionable party — and it
    ///     is what the caret is already on, since a built name has no span but
    ///     its origin's.
    ///     The subject is named rather than the counter nobody wrote, so the
    ///     one available rename is stated once at its source.
    /// </remarks>
    public override string Message
        => Built
         ? $"«{Name}» cannot be a name: the compiler builds names from it whose complete span also " +
           $"reads as a comparison, because «{Word}» is an operator between two values and the words " +
           "added in front supply the operand this name does not. No bracketing selects the built " +
           "name. Rename it."
         : $"«{Name}» cannot be a name: its complete span also reads as a comparison, because " +
           $"«{Word}» is an operator between two values. No bracketing selects the name reading — a " +
           "bracket inside the span selects the comparison, and one around it leaves the same two " +
           "readings inside. Respell it.";
}

/// <summary>
///     One shape declared twice with the same parameter types.
/// </summary>
///
/// <remarks>
///     Split from <see cref="Overloaded"/> because they expire differently and
///     only one of them expires. Two declarations of a shape whose parameter
///     types DIFFER are waiting for type-directed selection, and the message
///     saying so is temporary. Two whose types are the same are waiting for
///     nothing: no type information could ever tell them apart, so this is a
///     duplicate declaration and always will be.
///     <para>
///     Sharing a diagnostic meant landing the type checker would have required
///     picking the two apart under time pressure — which is what a ledger entry
///     recording only "expires" schedules.
///     </para>
///     <para>
///     Parameter NAMES are not part of the identity. «area of (radius => Number)»
///     and «area of (r => Number)» are the same declaration written twice, and a
///     caller cannot tell which they reached.
///     </para>
/// </remarks>
internal sealed class DuplicateSignature(Span primary, string pattern)
    : Finding(FindingKind.DuplicateSignature, primary)
{
    public string Pattern { get; } = pattern;

    public override string Message
        => $"«{Pattern}» is declared more than once with the same parameter types, so nothing could " +
           "ever choose between them. Remove one, or give them different types.";
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

/// <summary>A pattern shape already supplied by the language.</summary>
internal sealed class Supplied(Span primary, string pattern)
    : Finding(FindingKind.Supplied, primary)
{
    public string Pattern { get; } = pattern;

    public override string Message
        => $"«{Pattern}» is supplied by the language and cannot be declared again. Respell it.";
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

/// <summary>A type annotation whose words name no type.</summary>
///
/// <remarks>
///     <para>
///     The type half of a no-reading, and unlike the value half it is not
///     deferred. A value span that will not resolve may be an undeclared name, a
///     call that does not fit, or a phase this compiler has not built — and
///     reporting them as one would say the wrong one most of the time. A type
///     annotation has a single cause, because the table is complete at the
///     annotation: the words are not a type. So it is said where it is written,
///     once, however many uses share the mistake.
///     </para>
///     <para>
///     NO REPAIR, which is the rule rather than an omission. A repair is offered
///     when the program TEXT can select a reading, and here it cannot — no
///     bracketing turns a missing name into a present one. The remedies are a
///     declaration or a different word, and both are the author's.
///     </para>
///     <para>
///     "Nothing declares it" is the fact this states, and not "not in scope",
///     deliberately: once modules are scoped a second case appears — a type
///     declared in a module this one has not imported — whose remedy is the
///     import rather than a declaration. That arrives as its own finding, so this
///     sentence is not one anybody has to relearn.
///     </para>
/// </remarks>
internal sealed class UnknownType(Span primary, string name)
    : Finding(FindingKind.UnknownType, primary)
{
    public string Name { get; } = name;

    public override string Message
        => $"«{Name}» is not a type. Nothing declares it and the language supplies no such type. " +
           $"Declare it with «type {Name};», or name a type that exists.";
}

/// <summary>
///     A value whose inferred sort is not the type its declaration names — «var x =>
///     number = "text"». Reported at the value, since that is the half a reader
///     changes more often than the type.
/// </summary>
internal sealed class TypeMismatch(Span primary, string value, string declared)
    : Finding(FindingKind.TypeMismatch, primary)
{
    /// <summary>The value's inferred sort, spelled.</summary>
    public string Value { get; } = value;

    /// <summary>The declared type, as its words were written.</summary>
    public string Declared { get; } = declared;

    public override string Message
        => $"This value is a «{Value}», and «{Declared}» is declared. A value must have the type " +
           "its declaration names — change the value, or the type.";
}

/// <summary>
///     Two of a function's returns give different types, and no return type is written to
///     settle which the function hands back.
/// </summary>
internal sealed class DivergentReturns(Span primary, string value, string established)
    : Finding(FindingKind.DivergentReturns, primary)
{
    /// <summary>This return's inferred sort, spelled.</summary>
    public string Value { get; } = value;

    /// <summary>The sort an earlier return already fixed, spelled.</summary>
    public string Established { get; } = established;

    public override string Message
        => $"This return is a «{Value}», and an earlier return is a «{Established}». A function with no written " +
           "return type takes it from its returns, so they must agree — make them one type, or write it.";
}

/// <summary>
///     A type annotation past the resolver's ceiling — more words and symbols than
///     are read in one statement.
/// </summary>
///
/// <remarks>
///     The resolver refuses more than <see cref="Resolver.MaxLexemes"/> lexemes
///     rather than spend unbounded time on one statement, and a type annotation is
///     resolved the same way. Reported at the annotation because the alternative is
///     silence: the words resolve to no tree, so with no finding here an over-limit
///     annotation looks to a later pass exactly like an omitted one.
/// </remarks>
internal sealed class OversizeType(Span primary)
    : Finding(FindingKind.OversizeType, primary)
{
    public override string Message
        => $"This type annotation is more than {Resolver.MaxLexemes} words and symbols, which is past " +
           "what is read at once. No type is written this large; name one that exists.";
}

/// <summary>
///     A datum or datatype whose name is a pattern — «var provide (x)» — which only
///     a function may be.
/// </summary>
///
/// <remarks>
///     A parameter list makes a declaration a pattern, and a pattern is a callable
///     shape. A «var», «let», or «type» names a value or a type, not a call, so a
///     bracket in its name has nothing to bind and, cast to a function it is not,
///     would terminate the compiler rather than become a finding.
/// </remarks>
internal sealed class Parameterized(Span primary, string shape)
    : Finding(FindingKind.Parameterized, primary)
{
    public string Shape { get; } = shape;

    public override string Message
        => $"«{Shape}» has a parameter list, and only a function may take one. A «var», «let», or «type» is " +
           "named by words alone — name it without the bracket, or declare it as a function.";
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
