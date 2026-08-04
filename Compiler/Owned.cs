// Copyright © 2026 Eric Budai

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Ronin.Compiler;

/// <summary>
///     What a type keeps when a caller hands it a collection.
/// </summary>
///
/// <remarks>
///     <para>
///     Keep what this made, and copy everything else. The three rules before it
///     each asked the value a question and believed the answer: two named
///     concrete types, which is a guess at what a caller might build; the third
///     asked «ICollection.IsReadOnly», which says only that mutation is
///     unavailable THROUGH THAT INTERFACE and nothing about who else holds the
///     storage. «ReadOnlyCollection» over a list the caller kept, and
///     «ArraySegment» over an array the caller kept, both answer true and both
///     change underneath.
///     </para>
///     <para>
///     So ownership is not asked, it is ESTABLISHED. <see cref="Kept{T}"/> is
///     private to this class, its storage is its own, and nothing else can make
///     one — so "did this come from here" is answerable by type, where "can
///     anyone write to this" was never answerable at all.
///     </para>
///     <para>
///     The copy is still conditional, and now on a fact rather than a guess.
///     Copying every witness the resolver builds cost 27.6 MB against a 26 MB
///     ceiling; its producers make the owned value once instead, so a «Best»
///     built from one keeps it.
///     </para>
/// </remarks>
internal static class Owned
{
    public static Kept<T> Copy<T>(IReadOnlyList<T> values)
        => values is Kept<T> already ? already
         : values.Count is 0 ? Kept<T>.Empty
         : Kept<T>.Of(values);

    /// <summary>
    ///     An owned list of <paramref name="values"/>, for a producer that is
    ///     building one rather than handing one over.
    /// </summary>
    ///
    /// <remarks>
    ///     A SEQUENCE and not an array, so the storage is made here and no
    ///     caller can be holding it. The alternative — a factory taking an array
    ///     the caller just built — is the same promise-by-convention this class
    ///     exists to stop being, one call frame further out.
    ///
    ///     This is what «Copy» is for after: a producer that builds through this
    ///     is copied once, where building an ordinary list and letting the
    ///     consumer copy it pays again at every consumer it passes through.
    ///
    ///     No empty case, unlike «Copy». A producer calls this because it is
    ///     building something, and both that do are already past the test that
    ///     says there is more than one of it — an empty branch here would be one
    ///     nothing reaches.
    /// </remarks>
    public static Kept<T> Of<T>(IEnumerable<T> values) => Kept<T>.Of(values);

    /// <summary>The empty owned list, which is shared.</summary>
    ///
    /// <remarks>
    ///     A cell offering a reading with no witness is the common case, and it
    ///     should not build one to say so.
    /// </remarks>
    public static Kept<T> None<T>() => Kept<T>.Empty;

    /// <summary>A list whose storage is nobody else's.</summary>
    ///
    /// <remarks>
    ///     NAMED, so a producer can be declared to return it. Its constructor is
    ///     private and only <see cref="Owned"/> encloses it, so the type says
    ///     where the value came from — which makes "this is owned where it is
    ///     made" something the compiler checks rather than something a test
    ///     asserts about one specimen. A cell's tie branch is declared this way,
    ///     and an ordinary collection expression cannot satisfy it.
    /// </remarks>
    public sealed class Kept<T> : IReadOnlyList<T>
    {
        private Kept(T[] values) => this.values = values;

        /// <remarks>
        ///     A SEQUENCE, so the storage is made here and no caller can be
        ///     holding it. This is the only way to make one — the constructor is
        ///     private and an enclosing type cannot reach a nested type's
        ///     private members either — so "owned" is not a promise anybody
        ///     keeps, it is the only thing this can be.
        /// </remarks>
        internal static Kept<T> Of(IEnumerable<T> values) => new([.. values]);

        internal static Kept<T> Empty { get; } = Of([]);

        public int Count => values.Length;

        public T this[int index] => values[index];

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)values).GetEnumerator();

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        IEnumerator IEnumerable.GetEnumerator() => values.GetEnumerator();

        private readonly T[] values;
    }
}
