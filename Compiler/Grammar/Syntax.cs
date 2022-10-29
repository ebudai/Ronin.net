using Ronin.Compiler;
using Ronin.Lexicon;
using System.Collections;
using System.Xml.Linq;

namespace Ronin.Grammar;

internal interface IParsable
{
    public static abstract Syntax Parse(Parser parser);
}

internal abstract class Syntax
{
    protected internal ReadOnlyMemory<Token> Tokens { get; init; }
}

internal abstract class RepeatingSyntax<T> : Syntax, IEnumerable<T>
{
    internal T this[int index] => Elements[index];

    public IEnumerator<T> GetEnumerator() => Elements.Cast<T>().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Elements.GetEnumerator();

    protected internal T[] Elements;

    [ThreadStatic] protected internal static readonly List<T> buffer = new(64);
}

internal abstract class AggregateSyntax<T, TOpen, TElement, TSeparator, TClose> : RepeatingSyntax<TElement>, IParsable
    where TElement : IParsable
    where T : AggregateSyntax<T, TOpen, TElement, TSeparator, TClose>, new()
{
    public static Syntax Parse(Parser parser)
    {
        buffer.Clear();

        if (parser[0] is not TOpen) return null;

        ++parser.Cursor;

        while (parser.IsNotEmpty)
        {
            var syntax = TElement.Parse(parser);
            if (syntax is Error or null) return syntax;
            if (syntax is not TElement element) return Error.Parse(parser);
            buffer.Add(element);
            if (parser[0] is TClose)
            {
                ++parser.Cursor;
                break;
            }
            if (parser[0] is TSeparator)
            {
                ++parser.Cursor;
                continue;
            }
            return Error.Parse(parser);
        }

        return new T { Tokens = parser.Tokens, Elements = buffer.ToArray() };
    }
}

/*
[+ means and/or]

declare function - declarator then identifier then scope
declare datatype - modifiers then declarator then identifier then reference (optional algebra) then scope
x declare datum - declarator then parameter
identifier - name + parameters ...
reference - name + value ...
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