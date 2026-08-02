// Copyright © 2026 Eric Budai

using Ronin.Compiler;

namespace Ronin.Grammar;

/// <summary>
///     A reference that a definition follows, which is where a brace stops
///     being part of the reference.
/// </summary>
///
/// <remarks>
///     <para>
///     An anonymous value after a word is an argument — «thing 7 ("stuff")» is
///     one call — and a brace opens one. So «if c { 1 }» read as the reference
///     «c» applied to the list «{ 1 }» with no body left to find, and
///     «function f => Number { 1 }» lost its body to its return type the same
///     way. Both were malformed; «type T = Base {}» was worse and said nothing,
///     because a type may legally have no members, so the theft left nothing
///     behind to complain about.
///     </para>
///     <para>
///     Extracted because it was a hand-maintained list of the places that
///     needed it, and a hand-maintained list is a thing to be caught missing
///     from. It was: five scope headings had the rule and the two DECLARATION
///     headings did not, which is the same join and a different base class. The
///     question is never "is this a scope" but "does a definition follow this
///     reference", so that is what this asks.
///     </para>
/// </remarks>
internal static class Heading
{
    /// <summary>How every heading is parsed: from a parser, advancing it.</summary>
    internal delegate T Production<T>(ref Parser current);

    /// <summary>
    ///     Parses <paramref name="production"/> as a heading.
    /// </summary>
    ///
    /// <remarks>
    ///     Restored and not cleared, because a heading can contain one. «if» in
    ///     expression position is what makes that reachable — «if if a { b } { c }»
    ///     would otherwise have the inner heading end the outer one and the outer
    ///     body read as an argument again, which is the defect this exists to
    ///     remove, reintroduced by the feature it was written for.
    /// </remarks>
    public static T Of<T>(ref Parser current, Production<T> production)
    {
        var heading = current.Heading;

        current.Heading = true;

        var parsed = production(ref current);

        current.Heading = heading;

        return parsed;
    }
}
