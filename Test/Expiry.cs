// Copyright © 2026 Eric Budai

namespace Test;

/// <summary>
///     Which refusals the type checker will take back, marked before it lands.
/// </summary>
///
/// <remarks>
///     <para>
///     The self-ambiguity rule refuses a name whose own span has another
///     reading. That is the PRE-TYPE-CHECKER form of a narrower rule — a name
///     may not have another reading OF THE SAME TYPE IN THE SAME POSITION —
///     because filtering by well-typedness is elimination rather than a silent
///     pick, and elimination is a disambiguator brackets are not.
///     </para>
///     <para>
///     Every refusal this rule makes is therefore one of two things, and which
///     one is computable from the fixture text alone:
///     </para>
///     <list type="table">
///         <item>
///             <term>name against a pattern's call</term>
///             <description>the name's declared type against the pattern's return type</description>
///         </item>
///         <item>
///             <term>name against a comparison</term>
///             <description>the name's declared type against a truth</description>
///         </item>
///     </list>
///     <para>
///     Generated loop counters use the comparison row with the originating loop
///     variable's type. «old (_)» is absent because it is now a constrained
///     pattern and builds no source-level name.
///     </para>
///     <para>
///     Different types and the type checker recovers the name, so the refusal
///     — and the test asserting it — goes. Same type and neither the compiler
///     nor a reader has anything to go on, so it stays.
///     </para>
///     <para>
///     Tagged NOW, while the reasoning is in hand, so landing the type checker
///     reaches a named group rather than a reread of every fixture to work out
///     which of them was about this.
///     </para>
///     <para>
///     AND WHAT EACH BECOMES, which is the half a bare expiry leaves out. Almost
///     every approximation here is a NARROWING and not one of them disappears, so
///     a ledger that records only "expires" schedules a surprise: the group is
///     found, deleted, and the successor rule is written under time pressure by
///     whoever found it. The rows that WIDEN rather than narrow — the type walk's
///     unresolved base, and an unvalidated «fast» — are here for the same reason
///     from the other side: a silent accept schedules a deletion as much as a
///     silent refusal does.
///     </para>
///     <list type="table">
///         <listheader>
///             <term>rule</term>
///             <description>approximates → becomes</description>
///         </listheader>
///         <item>
///             <term>self-ambiguity — <c>Infixes</c> and <c>Shadowing</c> over names</term>
///             <description>
///                 approximates «a name may not have another reading OF THE SAME
///                 TYPE in the same position» → becomes that rule with the type
///                 clause restored. What is left is a name whose rival reading
///                 agrees with it in type, which nothing can eliminate and no
///                 bracket can select.
///             </description>
///         </item>
///         <item>
///             <term>duplicate shapes with differing parameter types</term>
///             <description>
///                 approximates type-directed selection at the call site →
///                 becomes a USE-SITE overload ambiguity, not a deletion. It
///                 needs a repair vocabulary that does not exist: brackets group
///                 rather than classify, so nothing selects between two
///                 declarations of one shape, and an expression-level type
///                 ascription is the prerequisite. Refusing at the declaration is
///                 what keeps that error out of the language meanwhile.
///             </description>
///         </item>
///         <item>
///             <term>«[]» is always a list</term>
///             <description>
///                 approximates the expected-type rule — «[]» is the empty
///                 lookup where a lookup is expected — which needs the type
///                 layer to have an expected type to consult. The resolver
///                 builds an empty square group as a LIST unconditionally, so
///                 the empty lookup has no way to be written today and
///                 «Lookup.Empty» is reachable only from a host caller. →
///                 becomes «[]» taking its kind from the type it is being read
///                 into, which is the same outward-in shape «return empty list»
///                 needs. Named here because "you cannot write an empty lookup"
///                 is exactly the gap someone builds a workaround for and then
///                 keeps after the rule lands.
///             </description>
///         </item>
///         <item>
///             <term>duplicate shapes with the SAME parameter types</term>
///             <description>
///                 approximates nothing and never expires — two identical
///                 declarations are a duplicate whatever the type layer knows.
///                 It has its own diagnostic now; it shared one with the row
///                 above, and being named here before they were told apart is
///                 what a successor column is for.
///             </description>
///         </item>
///         <item>
///             <term>a type definition's base is not resolved by the annotation walk</term>
///             <description>
///                 approximates full type-reference resolution — the walk reads
///                 the <c>Type.Unresolved</c> annotations and stops, so «type Car
///                 = Vehicle and { … }» reads «Vehicle» nowhere and an undeclared
///                 base is SILENT where an undeclared annotation is a finding. →
///                 becomes the same walk admitting one more node,
///                 <c>Algebra.Unresolved</c> beside <c>Type.Unresolved</c>, with
///                 no other machinery. One of the two rows here whose approximation
///                 is too LENIENT rather than too strict, and named for exactly
///                 that: a silent accept is the failure the annotation walk exists
///                 to prevent, one category over.
///             </description>
///         </item>
///         <item>
///             <term>«fast» on a non-number, and a duplicated «fast», compile cleanly</term>
///             <description>
///                 approximates no check at all — the annotation walk strips the
///                 modifier before resolving, so «fast truth» and «fast fast
///                 number» resolve to «truth» and «number» with nothing to say,
///                 because «fast» qualifies a NUMBER occurrence and knowing the
///                 occurrence resolved to «truth» is the resolved semantic type
///                 that does not exist yet. → becomes a target and duplicate check
///                 at the typed occurrence, in finding 1's checker. The second
///                 lenient row, and deferred for the same reason as the base: the
///                 check needs a type to check against.
///             </description>
///         </item>
///     </list>
/// </remarks>
internal static class Expiry
{
    /// <summary>The trait every tagged fixture carries, whichever way it goes.</summary>
    public const string Shrink = "Shrink";

    /// <summary>
    ///     The two readings differ in type, so eliminating by type leaves one and
    ///     this refusal stops being needed.
    /// </summary>
    public const string Expires = nameof(Expires);

    /// <summary>
    ///     The two readings have the same type in the same position, so nothing
    ///     eliminates either and the refusal is the whole answer.
    /// </summary>
    public const string Survives = nameof(Survives);
}
