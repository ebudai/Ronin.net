// Copyright © 2026 Eric Budai

using System;
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

    /// <summary>An owned list of exactly two, for a producer that has both.</summary>
    ///
    /// <remarks>
    ///     The VALUES and not a collection of them, which is what makes this
    ///     safe as well as cheap: the storage is made here, so no caller can be
    ///     holding it. A producer that already knew its two elements had to
    ///     manufacture something enumerable to say so, and that intermediate was
    ///     half the cost of the call.
    ///
    ///     A sequence overload was here for the producer that maps, and went
    ///     when the mapping moved inside. An «Of» nobody calls is a door with no
    ///     traffic and no test.
    /// </remarks>
    public static Kept<T> Of<T>(T first, T second) => Kept<T>.Of(first, second);

    /// <summary>
    ///     An owned list of what <paramref name="select"/> makes of each of
    ///     <paramref name="values"/>.
    /// </summary>
    ///
    /// <remarks>
    ///     The mapping happens INSIDE, so the final array is filled in place and
    ///     there is no iterator between the source and the storage. A producer
    ///     mapping an indexable input through «Select» paid for one either way.
    /// </remarks>
    public static Kept<T> Of<TSource, T>(IReadOnlyList<TSource> values, Func<TSource, T> select)
        => Kept<T>.Of(values, select);

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

        internal static Kept<T> Of(T first, T second) => new([first, second]);

        internal static Kept<T> Of<TSource>(IReadOnlyList<TSource> values, Func<TSource, T> select)
        {
            var made = new T[values.Count];

            for (var at = 0; at < made.Length; ++at) made[at] = select(values[at]);

            return new(made);
        }

        internal static Kept<T> Empty { get; } = Of([]);

        public int Count => values.Length;

        public T this[int index] => values[index];

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)values).GetEnumerator();

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        IEnumerator IEnumerable.GetEnumerator() => values.GetEnumerator();

        private readonly T[] values;
    }
}
