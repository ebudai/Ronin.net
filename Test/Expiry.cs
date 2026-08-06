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
///     Tagged NOW, while the reasoning is in hand. Landing the type checker is
///     then a deletion of a named group rather than a reread of every fixture
///     to work out which of them was about this, and a tag that states its own
///     expiry is the difference between an approximation being tracked and one
///     nobody dares touch.
///     </para>
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
