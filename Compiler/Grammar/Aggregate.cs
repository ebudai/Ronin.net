// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Ronin.Grammar;

/// <summary>
///     Parent class for all syntactical groupings
/// </summary>
/// 
/// <typeparam name="TParent">
///     The parent class
/// </typeparam>
/// 
/// <typeparam name="TOpen">
///     <see cref="Symbol"/> used to denote the start of the grouping - must be subclass of <see cref="Open"/>
/// </typeparam>
/// 
/// <typeparam name="TElement">
///     class to be aggregated - must be implementation of <see cref="IParsable{TElement}"/>
/// </typeparam>
/// 
/// <typeparam name="TSeparator">
///     <see cref="Symbol"/> used to separate each <typeparamref name="TElement"/> - must be subclass of <see cref="Punctuation"/>
/// </typeparam>
/// 
/// <typeparam name="TClose">
///     <see cref="Symbol"/> used to denote the completion of the grouping - must be subclass of <see cref="Close"/>
/// </typeparam>
internal abstract class Aggregate<TParent, TOpen, TElement, TSeparator, TClose> : Temporary, IList<TElement>
    where TParent : class, IList<TElement>, new()
    where TOpen : Open
    where TElement : IParsable<TElement>
    where TSeparator : Punctuation
    where TClose : Close
{
    public new static TParent Parse(ref Parser current)
    {
        Parser parser = current;

        if (parser.TryAdvance<TOpen>() is false) return null;

        TParent values = [];
        var closed = false;

        while (parser.IsNotFinished)
        {
            if (TElement.Parse(ref parser) is not TElement syntax)
            {
                // an element that will not parse is only acceptable where the
                // closer is, which is what makes «(a b)» and a truncated
                // aggregate different from an empty one
                if (parser.TryAdvance<TClose>() is false) return null;

                closed = true;
                break;
            }

            values.Add(syntax);

            // A separator must be followed by a space, so that no unspaced comma
            // is ever a separator and «1,234» is unambiguously one number. Without
            // it, inlining «count = 1» into «f(count,234)» silently turns two
            // arguments into one.
            if (parser.Token is Separator { Spaced: false }) return null;

            // A trailing separator is allowed — the guide's own examples use one
            // and it makes for cleaner diffs. An omitted one is not: «(a b)» has
            // to be rejected rather than read as two elements.
            if (parser.TryAdvance<TSeparator>() is false)
            {
                if (parser.TryAdvance<TClose>() is false) return null;

                closed = true;
                break;
            }
        }

        // running out of tokens is not the same as being closed
        if (closed is false) return null;

        current = parser;
        return values;
    }

    private readonly List<TElement> Values = [];

    #region list implementation
    [ExcludeFromCodeCoverage] public IEnumerator<TElement> GetEnumerator() => Values.GetEnumerator();
    [ExcludeFromCodeCoverage] IEnumerator IEnumerable.GetEnumerator() => Values.GetEnumerator();
    [ExcludeFromCodeCoverage] public int Count => Values.Count;
    [ExcludeFromCodeCoverage] public bool IsReadOnly => false;
    [ExcludeFromCodeCoverage] public TElement this[int index] { get => Values[index]; set => Values[index] = value; }
    [ExcludeFromCodeCoverage] public int IndexOf(TElement item) => Values.IndexOf(item);
    [ExcludeFromCodeCoverage] public void Insert(int index, TElement item) => Values.Insert(index, item);
    [ExcludeFromCodeCoverage] public void RemoveAt(int index) => Values.RemoveAt(index);
    [ExcludeFromCodeCoverage] public void Add(TElement item) => Values.Add(item);
    [ExcludeFromCodeCoverage] public void AddRange(IEnumerable<TElement> items) => Values.AddRange(items);
    [ExcludeFromCodeCoverage] public void Clear() => Values.Clear();
    [ExcludeFromCodeCoverage] public bool Contains(TElement item) => Values.Contains(item);
    [ExcludeFromCodeCoverage] public void CopyTo(TElement[] array, int arrayIndex) => Values.CopyTo(array, arrayIndex);
    [ExcludeFromCodeCoverage] public bool Remove(TElement item) => Values.Remove(item);
    [ExcludeFromCodeCoverage] public override bool Equals(object obj) => (obj as IEnumerable<TElement>)?.SequenceEqual(Values) ?? false;
    /// <remarks>
    ///     Over the elements, because <see cref="Equals"/> compares them. The
    ///     backing list's identity hash meant equal aggregates hashed differently.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    public override int GetHashCode()
    {
        System.HashCode hash = new();
        foreach (var value in Values) hash.Add(value);
        return hash.ToHashCode();
    }
    #endregion
}