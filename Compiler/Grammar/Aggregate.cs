// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System.Collections;

namespace Ronin.Grammar;

internal abstract class Aggregate<T> : AnonymousValue, IEnumerable<T>
{
    protected internal List<T> Values = new();

    public IEnumerator<T> GetEnumerator() => Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Values.GetEnumerator();
}

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
internal abstract class Aggregate<T, TOpen, TElement, TSeparator, TClose> : Aggregate<TElement>, IParsableSyntax<T>
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
                if (parser.TryAdvance<TClose>() is false) return null;
                break;
            }
            values.Add(syntax);
            if (parser.Token is TSeparator) parser.Advance();
        }

        return new T { Values = values, Source = parser.Commit(ref current) };
    }
}