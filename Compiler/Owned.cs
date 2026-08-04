// Copyright © 2026 Eric Budai

using System.Collections;
using System.Collections.Generic;

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
    public static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> values)
        => values is Kept<T> already ? already
         : values.Count is 0 ? Kept<T>.Empty
         : new Kept<T>([.. values]);

    /// <summary>A list whose storage is nobody else's.</summary>
    private sealed class Kept<T>(T[] values) : IReadOnlyList<T>
    {
        /// <remarks>
        ///     The empty one is shared. A resolver offering a reading with no
        ///     witness is the common case, and it should not allocate to say so.
        /// </remarks>
        public static Kept<T> Empty { get; } = new([]);

        public int Count => values.Length;

        public T this[int index] => values[index];

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)values).GetEnumerator();

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        IEnumerator IEnumerable.GetEnumerator() => values.GetEnumerator();
    }
}
