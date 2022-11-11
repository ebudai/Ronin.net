using Ronin.Compiler;

namespace Ronin.Grammar;

internal interface IParsable
{
    public static abstract Syntax Parse(ref Parser parser);
}

internal interface IParsable<T> : IParsable
{
    public static abstract T FromSyntax(Syntax syntax);
}

public abstract class Syntax
{
    protected internal SourceLocation[] Source { get; init; }
}

internal abstract class AggregateSyntax<T, TOpen, TElement, TSeparator, TClose> : Syntax, IParsable
    where TElement : class, IParsable<TElement>
    where T : AggregateSyntax<T, TOpen, TElement, TSeparator, TClose>, new()
{
    public static Syntax Parse(ref Parser context)
    {
        if (context.Current is not TOpen) return null;

        Parser parser = context;
        List<TElement> buffer = new();
        parser.Advance();

        while (parser.IsNotFinished)
        {
            var syntax = TElement.Parse(ref parser);
            if (syntax is Error) return syntax;
            if (syntax is null)
            {
                if (parser.Current is not TClose) return syntax;
                parser.Advance();
                break;
            }
            buffer.Add(TElement.FromSyntax(syntax));
            if (parser.Current is TSeparator) parser.Advance();
        }

        return new T { Values = buffer.ToArray(), Source = parser.Commit(ref context) };
    }

    protected internal TElement[] Values;
}

/*
[+ means and/or]

x declare function - modifiers declarator identifier scope
x declare datatype - modifiers declarator identifier { '=' reference } (optional) [algebra]s scope
x declare datum - declarator parameter
x identifier - name + parameters ...
x reference - name + value ...
x import - 'import' name
x partof - 'part of' name

x declarator - 'var' or 'constant' or 'let' or 'function' or 'datatype'
x modifiers - 'optional' or 'compiled' or 'persistent' or 'shared'
x name - word + wordable symbol ... [symbols don't need to be separated]
x assignment - name = value
x parameter - explicit - name => modifiers reference [datatype] = value (optionally) [initializer]
            - implicit - assignment
x scalar - literal ...
x value - scalar or aggregate or reference or declaration [ie: returns a function tearaway or datatype value etc]

x error - all until ';'

aggregates
x arguments - '(' value, ... ')'
x index - '[' value,... ']'
x scope - '{' value; ... '}'
x parameters - '(' parameter, ... ')'
*/