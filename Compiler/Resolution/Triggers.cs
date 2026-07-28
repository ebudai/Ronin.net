// Copyright © 2026 Eric Budai

using System.Collections.Generic;
using System.Linq;

namespace Ronin.Compiler;

/// <summary>
///     What a <c>when</c> is called.
/// </summary>
///
/// <remarks>
///     <para>
///     A <c>when</c> has no name in the syntax and every diagnostic that matters
///     needs one — the runaway message, and above all a cycle ring, whose whole
///     value is being readable. The answer needs no syntax: <strong>a when's name
///     is its trigger, rendered.</strong>
///     </para>
///     <para>
///     Position-derived names would give
///     <c>when@Player.cs:42 → when@Player.cs:57 → when@Respawn.cs:12</c>, which
///     is unreadable, and invented labels ask the reader to trust that «on
///     damage» is what changes health. The trigger says so, in the programmer's
///     own words, and it is greppable because it is what they wrote. The mode
///     comes along for free, since it is written in the source too.
///     </para>
/// </remarks>
internal static class Triggers
{
    /// <summary>How wide a rendered trigger may be before it is elided.</summary>
    private const int Width = 52;

    /// <summary>
    ///     Shortens from the middle, because both ends of a long condition are
    ///     the informative parts and the middle is the conjunction. The full text
    ///     stays available for a hover or a long-form error.
    /// </summary>
    public static string Elide(string name)
    {
        if (name.Length <= Width) return name;

        var keep = (Width - 5) / 2;
        return $"{name[..keep]} ... {name[^keep..]}";
    }

    /// <summary>
    ///     Distinct names for triggers that render identically, which is legal and
    ///     rare within one scope. Scope qualifies them first where there is one;
    ///     the ordinal is the last resort.
    /// </summary>
    public static IReadOnlyList<string> Distinct(IEnumerable<string> names)
    {
        Dictionary<string, int> seen = [];
        List<string> distinct = [];

        foreach (var name in names)
        {
            seen[name] = seen.GetValueOrDefault(name) + 1;
            distinct.Add(seen[name] is 1 ? name : $"{name} #{seen[name]}");
        }

        return distinct;
    }
}
