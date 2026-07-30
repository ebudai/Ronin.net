// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Grammar;

internal class Member : Value, IParsable<Member>
{
    public Modifiers Modifiers { get; init; }
    public Identifier Identifier { get; init; }

    public static new Member Parse(ref Parser current)
        => Datum.Parse(ref current)
        ?? Function.Parse(ref current)
        ?? Type.Parse(ref current) as Member;

    public class Unresolved : Member
    {
        public Reference Reference { get; init; }

        /// <remarks>
        ///     Whatever <c>Reference.Parse</c> returned, because it is the one
        ///     place that decides what a reference is. This used to ask again —
        ///     accepting a reference only if some component anywhere was a NAME —
        ///     and that second opinion was the whole defect: it refused «{ 1 } {
        ///     2 }» and accepted «{ 1 } { 2 } name», so a trailing word bought a
        ///     missing separator. The sequence is constrained where it is parsed.
        ///     <para>
        ///     The single-argument overload, which consumes nothing when the span
        ///     is a lone anonymous value. A condition is a reference or it is a
        ///     failure; only <c>Value.Parse</c> wants the value handed back.
        ///     </para>
        /// </remarks>
        public static new Unresolved Parse(ref Parser current)
            => Reference.Parse(ref current) is Reference reference ? new Unresolved { Reference = reference } : null;
    }
}
