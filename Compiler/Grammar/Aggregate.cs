// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Aggregates;

namespace Ronin.Grammar;

/// <summary>
///     Parent class for all groupings (<see cref="Arguments"/>, <see cref="Index"/>, <see cref="Parameters"/>, and <see cref="Scope"/>)
/// </summary>
/// 
/// <typeparam name="T">The child class</typeparam>
/// <typeparam name="TOpen"><see cref="Lexicon.Symbol"/> used to denote the start of the grouping</typeparam>
/// <typeparam name="TElement">class to be grouped</typeparam>
/// <typeparam name="TSeparator"><see cref="Lexicon.Symbol"/> used to separate each <see cref="{TElement}"/></typeparam>
/// <typeparam name="TClose"><see cref="Lexicon.Symbol"/> used to denote the completion of the grouping</typeparam>
internal abstract class Aggregate<T, TOpen, TElement, TSeparator, TClose> : Syntax, IParsable
    where T : Aggregate<T, TOpen, TElement, TSeparator, TClose>, new()
    where TElement : class, IParsable
{
    public static Syntax Parse(ref Parser context)
    {
        if (context.Current is not TOpen) return null;

        Parser parser = context;
        List<TElement> values = new();
        parser.Advance();

        while (parser.IsNotFinished)
        {
            var syntax = TElement.Parse(ref parser);
            if (syntax is Error) return syntax;
            if (syntax is null)
            {
                if (parser.Current is not TClose) return null;
                parser.Advance();
                break;
            }
            values.Add(syntax as TElement);
            if (parser.Current is TSeparator) parser.Advance();
        }

        return new T { Values = values, Source = parser.Commit(ref context) };
    }

    protected internal List<TElement> Values;
}