// Copyright © 2026 Eric Budai

using System.Collections.Generic;
using System.Collections.ObjectModel;
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
///     One description, and every consumer reads it. Source-level injections
///     appear in <see cref="All"/>; the runtime-only shadow descriptor is kept
///     outside that list because «old (_)» no longer inserts a symbol.
///     </para>
/// </remarks>
internal sealed class Injection
{
    private Injection(string cause, params string[] words)
    {
        Cause = cause;

        // COPIED and wrapped. A «params» array is the caller's, and this one is
        // a process-wide definition read in two ways: «Words» dynamically by
        // the resolver or reservation rules, «Prefix» computed once here.
        // Writing an element would split those apart — the pattern could become
        // «prior» while the graph node stayed «old x» — which is exactly the
        // two-independent-definitions failure this descriptor prevents.
        Words = new ReadOnlyCollection<string>([.. words]);

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
    ///     The runtime name of the previous value allocated by <c>old (_)</c>.
    /// </summary>
    ///
    /// <remarks>
    ///     This is no longer an injected source-level name. The resolver admits
    ///     <c>old x</c> only as the built-in pattern over a bare reactive
    ///     reference, and evaluation allocates this private graph node lazily.
    ///     Keeping its spelling here still gives the resolver and graph one
    ///     definition without putting the generated name in a symbol table.
    /// </remarks>
    public static Injection Shadow { get; } =
        new("the previous value selected by «old (_)»", "old") { Subject = "«a reactive name»" };

    /// <summary>The loop counter, named after the variable it counts for.</summary>
    public static Injection Counter { get; } =
        new("a loop's counter", "index", "of") { Subject = "«a loop variable»" };

    /// <summary>Every source-level name injection, which is what the registry and rules read.</summary>
    public static IReadOnlyList<Injection> All { get; } = [Counter];
}
