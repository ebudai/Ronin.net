// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Compound;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal abstract class Aggregate<T> : Anonymous
{
    protected internal List<T> Values;
}
/// <summary>
///     Parent class for all groupings (<see cref="Inputs"/>, <see cref="Ordinal"/>, <see cref="Parameters"/>, and <see cref="Definition"/>)
/// </summary>
/// 
/// <typeparam name="T">
///     The aggregated class
/// </typeparam>
/// 
/// <typeparam name="TOpen">
///     <see cref="Symbol"/> used to denote the start of the grouping - must be subclass of <see cref="Open"/>
/// </typeparam>
/// 
/// <typeparam name="TElement">
///     class to be grouped - must be implementation of <see cref="IParsableSyntax{TElement}"/>
/// </typeparam>
/// 
/// <typeparam name="TSeparator">
///     <see cref="Symbol"/> used to separate each <typeparamref name="TElement"/>
/// </typeparam>
/// 
/// <typeparam name="TClose">
///     <see cref="Symbol"/> used to denote the completion of the grouping - must be subclass of <see cref="Close"/>
/// </typeparam>
internal abstract class Aggregate<T, TOpen, TElement, TSeparator, TClose> : Aggregate<TElement>, IParsableSyntax<T>
    where T : Aggregate<T, TOpen, TElement, TSeparator, TClose>, new()
    where TOpen : Symbol
    where TElement : Syntax, IParsableSyntax<TElement>
    where TSeparator : Symbol
    where TClose : Symbol
{
    public new static T Parse(scoped ref Parser current)
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