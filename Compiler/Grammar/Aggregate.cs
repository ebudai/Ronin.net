// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Ronin.Grammar;

/// <summary>
///     Parent class for all groupings (<see cref="Inputs"/>, <see cref="Indexer"/>, <see cref="Parameters"/>, and <see cref="Context"/>)
/// </summary>
/// 
/// <typeparam name="T">
///     The aggregated class
/// </typeparam>
/// 
/// <typeparam name="TOpen">
///     <see cref="Symbol"/> used to denote the start of the grouping - must be subclass of <see cref="Punctuation"/>
/// </typeparam>
/// 
/// <typeparam name="TElement">
///     class to be grouped - must be implementation of <see cref="IParsableSyntax{TElement}"/> and subclass of <see cref="Syntax"/>
/// </typeparam>
/// 
/// <typeparam name="TSeparator">
///     <see cref="Symbol"/> used to separate each <typeparamref name="TElement"/> - must be subclass of <see cref="Punctuation"/>
/// </typeparam>
/// 
/// <typeparam name="TClose">
///     <see cref="Symbol"/> used to denote the completion of the grouping - must be subclass of <see cref="Punctuation"/>
/// </typeparam>
internal abstract class Aggregate<T, TOpen, TElement, TSeparator, TClose> : Value.Anonymous, IEnumerable<TElement>, IList<TElement>, IParsableSyntax<T>
    where T : Aggregate<T, TOpen, TElement, TSeparator, TClose>, new()
    where TOpen : Punctuation
    where TElement : Syntax, IParsableSyntax<TElement>
    where TSeparator : Punctuation
    where TClose : Punctuation
{
    public new static T Parse(ref Parser current)
    {
        if (current.Token is not TOpen) return null;

        Parser parser = current;
        List<TElement> values = new();
        parser.Advance();

        while (parser.IsNotFinished)
        {
            var syntax = TElement.Parse(ref parser);
            if (syntax is null)
            {
                if (parser.TryParse<TClose>() is null) return null;
                break;
            }
            values.Add(syntax);
            if (parser.Token is TSeparator) parser.Advance();
        }

        var parsed = new T { Source = parser.Commit(ref current) };
        parsed.AddRange(values);
        return parsed;
    }

    public IEnumerator<TElement> GetEnumerator() => Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Values.GetEnumerator();

    public int Count => Values.Count;

    public bool IsReadOnly => false;

    public TElement this[int index]
    {
        get => Values[index];
        set => Values[index] = value;
    }

    public int IndexOf(TElement item) => Values.IndexOf(item);

    public void Insert(int index, TElement item) => Values.Insert(index, item);

    public void RemoveAt(int index) => Values.RemoveAt(index);

    public void Add(TElement item) => Values.Add(item);

    public void AddRange(IEnumerable<TElement> items) => Values.AddRange(items);

    public void Clear() => Values.Clear();

    public bool Contains(TElement item) => Values.Contains(item);

    public void CopyTo(TElement[] array, int arrayIndex) => Values.CopyTo(array, arrayIndex);

    public bool Remove(TElement item) => Values.Remove(item);

    public override bool Equals(object obj) => (obj as IEnumerable<TElement>)?.SequenceEqual(Values) ?? false;

    public override int GetHashCode() => Values.GetHashCode();

    private readonly List<TElement> Values = new();
}