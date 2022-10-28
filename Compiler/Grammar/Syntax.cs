using Ronin.Compiler;
using Ronin.Lexicon;
using System.Collections;

namespace Ronin.Grammar;

internal interface IParsable
{
    public static abstract Syntax Parse(Parser parser);
}

internal abstract class Syntax
{
    protected internal ReadOnlyMemory<Token> Tokens { get; init; }
}

internal abstract class AggregateSyntax<T, TOpen, TElement, TSeparator, TClose> : Syntax, IParsable, IEnumerable<TElement>
    where TElement : IParsable
    where T : AggregateSyntax<T, TOpen, TElement, TSeparator, TClose>, new()
{
    internal TElement this[int index] => _elements[index];

    public static Syntax Parse(Parser parser)
    {
        List<TElement> elements = _container.Value;
        elements.Clear();

        if (parser[0] is not TOpen) return null;

        ++parser.Cursor;

        while (parser.IsNotEmpty)
        {
            var syntax = TElement.Parse(parser);
            if (syntax is Error or null) return syntax;
            if (syntax is not TElement element) return Error.Parse(parser);
            elements.Add(element);
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

        return new T { Tokens = parser.Tokens, _elements = elements.ToArray() };
    }

    public IEnumerator<TElement> GetEnumerator() => _elements.Cast<TElement>().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _elements.GetEnumerator();

    private TElement[] _elements;

    private static readonly ThreadLocal<List<TElement>> _container = new(() => new(64));
}

/*
[+ means and/or]

declare function - declarator then identifier then scope
declare datatype - modifiers then declarator then identifier then reference (optional algebra) then scope
declare datum - declarator then parameter
identifier - name + parameters ...
reference - name + value ...
x import - 'import' then name
x partof - 'part of' then name

x declarator - 'var' or 'constant' or 'let' or 'function' or 'datatype'
x modifiers - 'optional' or 'compiled' or 'persistent' or 'shared'
x name - word + wordable symbol ... [symbols don't need to be separated]
x parameter - explicit - name then => then modifiers then reference [datatype] then = then value (optionally) [initializer]
            - implicit - name then = then value
scalar - literal ...
value - scalar or aggregate or reference or declaration [ie: returns a function tearaway or datatype value etc]
statement - value then ';'

x error - all until ';'

aggregates
x arguments - '(' then value, ... then ')'
x index - '[' then value,... then ']'
x scope - '{' then value; ... then '}'
x parameters - '(' then parameter, ... then ')'
*/