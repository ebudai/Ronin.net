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

        // Every kind of nesting in the grammar comes through here, so this is
        // where it is bounded. A file of fifty thousand open braces recursed
        // straight through the stack, and a StackOverflowException cannot be
        // caught — no error handling downstream could have turned it into a
        // diagnostic.
        if (Parser.Nest() is false) return null;

        try
        {
            return Parsed(ref current, ref parser);
        }
        finally
        {
            Parser.Unnest();
        }
    }

    /// <summary>
    ///     Whether this aggregate sequences statements, which are separated by a
    ///     terminator, rather than values, which are separated by commas.
    /// </summary>
    private static bool Statements => typeof(TSeparator) == typeof(Terminal);

    private static TParent Parsed(ref Parser current, ref Parser parser)
    {
        TParent values = [];
        var closed = false;

        // Inside brackets there is nothing for a brace to be ambiguous with, so
        // a heading stops at the opener. That is what keeps a braced value
        // available as an argument — «if takes ({ 1 }) { … }» — and it is also
        // what lets a body hold an ordinary list.
        //
        // Restored on the way out, because the parser is a struct written back
        // over the caller's: leaving it cleared ended the caller's heading at
        // the first bracket in it, so «if c (x) { 1 }» lost its body to the
        // condition and every argument list undid the rule one call later.
        var heading = parser.Heading;

        parser.Heading = false;

        while (parser.IsNotFinished)
        {
            var started = parser;

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
                // Unless this is a sequence of STATEMENTS and the element ended
                // with a block, which already says where it stops. Requiring «;»
                // after «if x { … }» meant a braced statement could be the LAST
                // thing in a block and nothing else — «function f { if x {
                // return 1; } return 2; }» did not compile, which is most
                // programs.
                //
                // Scoped to the separator, because this class also parses
                // comma-delimited values and the exemption leaked into them:
                // «var nested = { { 1 } { 2 } };» was accepted with the comma
                // missing. A brace ends a statement; it does not end a list
                // element.
                if (Statements && Sequence.Elides(started, parser)) continue;

                if (parser.TryAdvance<TClose>() is false) return null;

                closed = true;
                break;
            }
        }

        // running out of tokens is not the same as being closed
        if (closed is false) return null;

        parser.Heading = heading;
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
