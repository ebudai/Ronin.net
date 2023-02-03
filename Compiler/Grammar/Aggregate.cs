// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

/// <summary>
///     Parent class for all groupings (<see cref="Arguments"/>, <see cref="Ordinal"/>, <see cref="Parameters"/>, and <see cref="Scope"/>)
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
///     class to be grouped - must be implementation of <see cref="IParsable{TElement}"/>
/// </typeparam>
/// 
/// <typeparam name="TSeparator">
///     <see cref="Symbol"/> used to separate each <typeparamref name="TElement"/>
/// </typeparam>
/// 
/// <typeparam name="TClose">
///     <see cref="Symbol"/> used to denote the completion of the grouping - must be subclass of <see cref="Close"/>
/// </typeparam>
internal abstract class Aggregate<T, TOpen, TElement, TSeparator, TClose> : Syntax, Compiler.IParsable<T>
    where T : Aggregate<T, TOpen, TElement, TSeparator, TClose>, new()
    where TOpen : Open
    where TElement : Compiler.IParsable<TElement>
    where TSeparator : Symbol
    where TClose : Close
{
    public static T Parse(ref Parser context)
    {
        if (context.CurrentToken is not TOpen) return null;

        Parser parser = context;
        List<TElement> values = new();
        parser.Advance();

        while (parser.IsNotFinished)
        {
            var syntax = TElement.Parse(ref parser);
            if (syntax is null)
            {
                if (parser.CurrentToken is not TClose) return null;
                parser.Advance();
                break;
            }
            values.Add(syntax);
            if (parser.CurrentToken is TSeparator) parser.Advance();
        }

        return new T { Values = values, Source = parser.Commit(ref context) };
    }

    protected internal List<TElement> Values;
}