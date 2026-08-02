// Copyright © 2026 Eric Budai

using System.Collections.Generic;
using System.Linq;

namespace Ronin.Compiler;

/// <summary>
///     A name the compiler builds, described once.
/// </summary>
///
/// <remarks>
///     <para>
///     There were three copies of this: the builders that make the names, the
///     rule that protects the words they are made from, and the registry that
///     lists them. Each was written by hand from the others, so the test that
///     checked them checked the SAMPLE against the rule rather than the
///     implementation against either — a real injector left out of the registry
///     would have kept it green, which is the same defect as a hand-built token
///     chain standing in for source.
///     </para>
///     <para>
///     One description, and the three consume it. Adding an injection is adding
///     an entry; forgetting to protect its words is a failing test rather than a
///     trap someone finds in the field.
///     </para>
/// </remarks>
internal sealed class Injection
{
    private Injection(string cause, params string[] words)
    {
        Cause = cause;
        Words = words;
        Prefix = string.Concat(words.Select(word => word + " "));
    }

    /// <summary>What causes the compiler to build this name.</summary>
    public string Cause { get; }

    /// <summary>The compiler's own words in it, which no pattern may use as glue.</summary>
    public IReadOnlyList<string> Words { get; }

    /// <summary>The words and a space, which is how a name is built from them.</summary>
    public string Prefix { get; }

    /// <summary>The injected name for <paramref name="name"/>.</summary>
    public string Of(string name) => Prefix + name;

    /// <summary>The injected name's words, for a rule that counts words.</summary>
    public IReadOnlyList<string> Of(IReadOnlyList<string> words) => [.. Words, .. words];

    /// <summary>How the registry shows it.</summary>
    public string Shape => Prefix + Subject;

    private string Subject { get; init; }

    /// <summary>
    ///     The previous value of a cell that has one.
    /// </summary>
    ///
    /// <remarks>
    ///     Every mutable declaration, not only a reactive one — the registry used
    ///     to say "a reactive declaration's previous value" while
    ///     <c>Declarations.Cell</c> injected it for every non-constant datum. A
    ///     constant is excepted because its previous value is provably its
    ///     current one.
    /// </remarks>
    public static Injection Shadow { get; } =
        new("the previous value of a mutable declaration", "old") { Subject = "«a name»" };

    /// <summary>The loop counter, named after the variable it counts for.</summary>
    public static Injection Counter { get; } =
        new("a loop's counter", "index", "of") { Subject = "«a loop variable»" };

    /// <summary>Every injection there is, which is what the registry and the rule read.</summary>
    public static IReadOnlyList<Injection> All { get; } = [Shadow, Counter];
}
