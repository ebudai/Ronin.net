using Ronin.Compiler;
using System.Collections;

namespace Ronin.Grammar;

internal interface IParsable
{
    public static abstract Syntax Parse(ref Parser context);
}

internal interface IParsable<T> : IParsable
{
    public static abstract T FromSyntax(Syntax syntax);
}

public abstract class Syntax
{
    protected internal virtual (int index, int length) Tokens { get; init; }
}

internal abstract class RepeatingSyntax<T> : Syntax, IEnumerable<T>
{
    public T this[int index] => Values[index];

    public IEnumerator<T> GetEnumerator() => Values.Cast<T>().GetEnumerator(); // the Cast<T> allows us to call GetEnumerator for the array and remain generic

    IEnumerator IEnumerable.GetEnumerator() => Values.GetEnumerator();

    protected internal T[] Values;
}

internal abstract class AggregateSyntax<T, TOpen, TElement, TSeparator, TClose> : RepeatingSyntax<TElement>, IParsable
    where TElement : IParsable<TElement>
    where T : AggregateSyntax<T, TOpen, TElement, TSeparator, TClose>, new()
{
    public static Syntax Parse(ref Parser context)
    {
        List<TElement> buffer = new();

        if (context[0] is not TOpen) return null;

        Parser parser = context;

        ++parser.Cursor;

        while (parser.IsNotEmpty)
        {
            var syntax = TElement.Parse(ref parser);
            if (syntax is Error or null) return syntax;
            if (TElement.FromSyntax(syntax) is not TElement element) return Error.Parse(ref context);
            buffer.Add(element);
            ref readonly var token = ref parser[0];
            if (token is TClose)
            {
                ++parser.Cursor;
                break;
            }
            if (token is TSeparator)
            {
                ++parser.Cursor;
                continue;
            }
            return Error.Parse(ref parser);
        }

        return new T { Tokens = parser.GetTokens(ref context), Values = buffer.ToArray() };
    }
}

/*
[+ means and/or]

x declare function - modifiers then declarator then identifier then scope
x declare datatype - modifiers then declarator then identifier then { '=' then reference } (optional algebra) then scope
x declare datum - declarator then parameter
x identifier - name + parameter or parameters ...
x reference - name + value ...
x import - 'import' then name
x partof - 'part of' then name

x declarator - 'var' or 'constant' or 'let' or 'function' or 'datatype'
x modifiers - 'optional' or 'compiled' or 'persistent' or 'shared'
x name - word + wordable symbol ... [symbols don't need to be separated]
x parameter - explicit - name then => then modifiers then reference [datatype] then = then value (optionally) [initializer]
            - implicit - name then = then value
x scalar - literal ...
x value - scalar or aggregate or reference or declaration [ie: returns a function tearaway or datatype value etc]

x error - all until ';'

aggregates
x arguments - '(' then value, ... then ')'
x index - '[' then value,... then ']'
x scope - '{' then value; ... then '}'
x parameters - '(' then parameter, ... then ')'
*/