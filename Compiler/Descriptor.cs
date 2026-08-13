// Copyright © 2026 Eric Budai

using System.Collections.Generic;

namespace Ronin.Compiler;

/// <summary>
///     Something the language supplies, described once.
/// </summary>
///
/// <remarks>
///     <para>
///     The fourth time this shape has been needed and the third time it has been
///     built. <see cref="Injection"/> says why in one line: there were three
///     hand-written copies of what the compiler injects, so the test that checked
///     them checked the sample against the rule rather than the implementation
///     against either, and a real injector left out of the registry would have
///     kept it green.
///     </para>
///     <para>
///     Documentation is the same failure waiting to happen. A hand-maintained
///     page per builtin drifts from the builtin, and nothing says when — which is
///     four documented instances on this project already, one of them mine. So
///     the reference text is a FIELD and <c>docs/reference.md</c> is generated
///     from these, which is the only fix that does not depend on somebody
///     remembering.
///     </para>
///     <para>
///     REQUIRED BY THE TYPE rather than by a test, where the type can reach.
///     <see cref="Summary"/> is a constructor parameter, so an entry cannot be
///     added without one and the thought never occurs; that a summary is empty,
///     or that a <see cref="SeeAlso"/> names nothing, are cross-cutting facts no
///     single record can see, and those are tested. Make the wrong state
///     unrepresentable before making it detectable.
///     </para>
///     <para>
///     And this list is the first concrete form of one-table-with-kinds: a
///     pattern and a literal are two entries here rather than two collections,
///     which is what <c>Builtins</c> and <c>Truths</c> now derive from. The kind
///     field belongs on this record when it arrives, which is much cheaper than a
///     second record to put it on.
///     </para>
/// </remarks>
internal sealed record Descriptor
{
    private Descriptor(string summary, string name, Pattern shape)
    {
        Summary = summary;
        Name = name;
        Shape = shape;
    }

    /// <summary>A supplied pattern — something with a hole to fill.</summary>
    public static Descriptor Shaped(string summary, Pattern shape) => new(summary, shape.ToString(), shape);

    /// <summary>A supplied name — a nullary entry, which reserves its own spelling and nothing else.</summary>
    public static Descriptor Spelled(string summary, string word) => new(summary, word, null);

    /// <summary>One sentence: what it does.</summary>
    public string Summary { get; }

    /// <summary>How the reference lists it, and what a <see cref="SeeAlso"/> names.</summary>
    public string Name { get; }

    /// <summary>The pattern, where it is one. Null for a name.</summary>
    public Pattern Shape { get; }

    /// <summary>
    ///     Whether this supplies a value or a type.
    /// </summary>
    ///
    /// <remarks>
    ///     ONE LIST for both, because the table they seed is one table — a type
    ///     name is a name, told apart by its kind rather than by living somewhere
    ///     else. What the kind is for here is the derivations: every one of them
    ///     asks this list a question, and «what are the truth literals» wants a
    ///     different answer from «what spellings does the language supply».
    /// </remarks>
    public SymbolKind Kind { get; init; } = SymbolKind.Value;

    /// <summary>The spellings, one per arity, where more than one reads differently.</summary>
    ///
    /// <remarks>
    ///     Owned where it is set, by the one rule everything else here follows: a
    ///     collection expression assigned to a read-only list is an array, which
    ///     reports itself read-only and assigns through a cast. A description of
    ///     the language is not something a caller gets to edit afterwards.
    /// </remarks>
    public IReadOnlyList<string> Forms
    {
        get => forms;
        init => forms = Owned.Copy(value);
    }

    /// <summary>Where it may be written, when that is not everywhere.</summary>
    public string Legal { get; init; }

    /// <summary>
    ///     Other entries a reader of this one wants.
    /// </summary>
    ///
    /// <remarks>
    ///     Entry NAMES rather than prose, so the reference can render them as
    ///     links without parsing anything and a name that stops existing is a
    ///     failing test rather than a dead sentence. The whole reason the pair
    ///     this was built for exists is that each names the other, and a
    ///     cross-reference that can rot is one that will.
    /// </remarks>
    public IReadOnlyList<string> SeeAlso
    {
        get => also;
        init => also = Owned.Copy(value);
    }

    private readonly IReadOnlyList<string> forms = Owned.Copy<string>([]);
    private readonly IReadOnlyList<string> also = Owned.Copy<string>([]);
}
