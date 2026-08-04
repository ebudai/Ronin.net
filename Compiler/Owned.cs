// Copyright © 2026 Eric Budai

using System;
using System.Collections.Generic;

namespace Ronin.Compiler;

/// <summary>
///     What a type keeps when a caller hands it a collection.
/// </summary>
///
/// <remarks>
///     <para>
///     Copy when something can write to it, and keep it when nothing can. The
///     two shapes this replaces named CONCRETE TYPES — «List» and an array —
///     which is a guess at what a caller might build rather than a question
///     about what it built: «Collection&lt;T&gt;» is an ordinary writable
///     «IReadOnlyList» and neither name matched it, so it passed through and a
///     later write by the caller changed what the compiler held.
///     </para>
///     <para>
///     Asked of the object and not of the declared type, which is the same
///     lesson the return-boundary test learned twice. And an ARRAY is asked
///     about separately: it reports «IsReadOnly» as true through
///     «ICollection» while one cast assigns an element.
///     </para>
///     <para>
///     The copy is conditional because it has to be. Copying every witness the
///     resolver builds costs 27.6 MB against a 26 MB ceiling; copying only what
///     is writable costs nothing, because nothing the resolver builds is.
///     </para>
/// </remarks>
internal static class Owned
{
    public static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> values)
        => Writable(values) ? [.. values] : values;

    private static bool Writable<T>(IReadOnlyList<T> values)
        => values is Array

        // Cannot be asked, so it is copied. Every read-only shape in the
        // compiler answers «ICollection» today; one that does not is one this
        // has no way to trust, and the safe answer to "I cannot tell" is the
        // copy rather than the promise.
        || values is not ICollection<T> asked

        || asked.IsReadOnly is false;
}
