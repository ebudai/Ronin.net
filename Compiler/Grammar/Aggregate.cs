// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Aggregates;
using Ronin.Grammar.Errors;
using Ronin.Lexicon;

namespace Ronin.Grammar;

/// <summary>
///     Parent class for all groupings (<see cref="Arguments"/>, <see cref="Index"/>, <see cref="Parameters"/>, and <see cref="Scope"/>)
/// </summary>
/// 
/// <typeparam name="T">
///     The aggregated class
/// </typeparam>
/// 
/// <typeparam name="TOpen">
///     <see cref="Symbol"/> used to denote the start of the grouping
/// </typeparam>
/// 
/// <typeparam name="TElement">
///     class to be grouped
/// </typeparam>
/// 
/// <typeparam name="TSeparator">
///     <see cref="Symbol"/> used to separate each <typeparamref name="TElement"/>
/// </typeparam>
/// 
/// <typeparam name="TClose">
///     <see cref="Symbol"/> used to denote the completion of the grouping
/// </typeparam>
internal abstract class Aggregate<T, TOpen, TElement, TSeparator, TClose> : Syntax, Compiler.IParsable<T>
    where T : Aggregate<T, TOpen, TElement, TSeparator, TClose>, new()
    where TOpen : Symbol
    where TElement : Compiler.IParsable<TElement>
    where TSeparator : Symbol
    where TClose : Symbol
{
    public static T Parse(ref Parser context)
    {
        if (context.Current is not TOpen) return null;

        Parser parser = context;
        List<TElement> values = new();
        parser.Advance();

        while (parser.IsNotFinished)
        {
            var syntax = TElement.Parse(ref parser);
            if (syntax is null)
            {
                if (parser.Current is not TClose) throw new ExpectedSyntaxError<TSeparator, TClose>(ref context);
                parser.Advance();
                break;
            }
            values.Add(syntax);
            if (parser.Current is TSeparator) parser.Advance();
        }

        return new T { Values = values, Source = parser.Commit(ref context) };
    }

    protected internal List<TElement> Values;
}