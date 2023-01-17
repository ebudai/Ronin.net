using Ronin.Grammar;

namespace Ronin.Compiler;

public abstract class Syntax
{
    protected internal SourceLocation[] Source { get; init; }
}

internal abstract class AggregateSyntax<T, TOpen, TElement, TSeparator, TClose> : Syntax, IParsable
    where T : AggregateSyntax<T, TOpen, TElement, TSeparator, TClose>, new()
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

internal abstract class AggregateElementSyntax<T, TOpen, TElement, TSeparator, TClose> : Syntax, IParsable
    where T : AggregateElementSyntax<T, TOpen, TElement, TSeparator, TClose>, new()
    where TElement : IParsable, IElement<TElement>
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
            values.Add(syntax);
            if (parser.Current is TSeparator) parser.Advance();
        }

        return new T { Values = values, Source = parser.Commit(ref context) };
    }

    protected internal List<TElement> Values;
}
/*

[+ means and/or]

x declare function - modifiers 'function' identifier scope
x declare datatype - modifiers 'datatype' identifier { '=' reference } (optional) [algebra] scope
x declare datum - declarator parameter
x identifier - name + parameters ...
x reference - name + value ...
x import - 'import' name
x partof - 'part of' name

x declarator - 'var' or 'constant' or 'let'
x modifiers - 'optional' or 'compiled' or 'persistent' or 'shared'
x name - word + wordable symbol ... [symbols don't need to be separated]
x assignment - name = value
x parameter - explicit - name => modifiers reference [datatype] { '=' value } (optionally) [initializer]
            - implicit - assignment
x scalar - literal ...
x value - scalar or aggregate or reference or declaration [ie: returns a function tearaway or datatype value etc]

x error - all until ';'

aggregates
x arguments - '(' value, ... ')'
x index - '[' value,... ']'
x scope - '{' value; ... '}'
x parameters - '(' parameter + assignment, ... ')'

*/