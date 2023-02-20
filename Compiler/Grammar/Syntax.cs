// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Grammar;

internal abstract class Syntax
{
    protected internal Token[] Source { get; init; }
}

internal abstract class CompositeSyntax : Syntax
{
    public bool IsNot<T>() where T : Syntax => value is not T;

    protected internal Syntax value;
}

internal abstract class CompositeSyntax<T, T0, T1> : CompositeSyntax, Compiler.IParsable<T>
    where T : CompositeSyntax, Compiler.IParsable<T>, new()
    where T0 : Syntax, Compiler.IParsable<T0>
    where T1 : Syntax, Compiler.IParsable<T1>
{
    public static T Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = T0.Parse(ref parser)
            ?? T1.Parse(ref parser) as Syntax;

        if (syntax is null) return null;

        return new T { value = syntax, Source = parser.Commit(ref context) };
    }

    public static implicit operator T0(CompositeSyntax<T, T0, T1> value) => value.value as T0;
    public static implicit operator T1(CompositeSyntax<T, T0, T1> value) => value.value as T1;
}

internal abstract class CompositeSyntax<T, T0, T1, T2> : CompositeSyntax, Compiler.IParsable<T>
    where T : CompositeSyntax, Compiler.IParsable<T>, new()
    where T0 : Syntax, Compiler.IParsable<T0>
    where T1 : Syntax, Compiler.IParsable<T1>
    where T2 : Syntax, Compiler.IParsable<T2>
{
    public static T Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = T0.Parse(ref parser)
            ?? T1.Parse(ref parser)
            ?? T2.Parse(ref parser) as Syntax;

        if (syntax is null) return null;

        return new T { value = syntax, Source = parser.Commit(ref context) };
    }

    public static implicit operator T0(CompositeSyntax<T, T0, T1, T2> value) => value.value as T0;
    public static implicit operator T1(CompositeSyntax<T, T0, T1, T2> value) => value.value as T1;
    public static implicit operator T2(CompositeSyntax<T, T0, T1, T2> value) => value.value as T2;
}

[ExcludeFromCodeCoverage]
internal abstract class CompositeSyntax<T, T0, T1, T2, T3> : CompositeSyntax, Compiler.IParsable<T>
    where T : CompositeSyntax, Compiler.IParsable<T>, new()
    where T0 : Syntax, Compiler.IParsable<T0>
    where T1 : Syntax, Compiler.IParsable<T1>
    where T2 : Syntax, Compiler.IParsable<T2>
    where T3 : Syntax, Compiler.IParsable<T3>
{
    public static T Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = T0.Parse(ref parser)
            ?? T1.Parse(ref parser)
            ?? T2.Parse(ref parser) 
            ?? T3.Parse(ref parser) as Syntax;

        if (syntax is null) return null;

        return new T { value = syntax, Source = parser.Commit(ref context) };
    }

    public static implicit operator T0(CompositeSyntax<T, T0, T1, T2, T3> value) => value.value as T0;
    public static implicit operator T1(CompositeSyntax<T, T0, T1, T2, T3> value) => value.value as T1;
    public static implicit operator T2(CompositeSyntax<T, T0, T1, T2, T3> value) => value.value as T2;
    public static implicit operator T3(CompositeSyntax<T, T0, T1, T2, T3> value) => value.value as T3;
}

[ExcludeFromCodeCoverage]
internal abstract class CompositeSyntax<T, T0, T1, T2, T3, T4> : CompositeSyntax, Compiler.IParsable<T>
    where T : CompositeSyntax, Compiler.IParsable<T>, new()
    where T0 : Syntax, Compiler.IParsable<T0>
    where T1 : Syntax, Compiler.IParsable<T1>
    where T2 : Syntax, Compiler.IParsable<T2>
    where T3 : Syntax, Compiler.IParsable<T3>
    where T4 : Syntax, Compiler.IParsable<T4>
{
    public static T Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = T0.Parse(ref parser)
            ?? T1.Parse(ref parser)
            ?? T2.Parse(ref parser)
            ?? T3.Parse(ref parser)
            ?? T4.Parse(ref parser) as Syntax;

        if (syntax is null) return null;

        return new T { value = syntax, Source = parser.Commit(ref context) };
    }

    public static implicit operator T0(CompositeSyntax<T, T0, T1, T2, T3, T4> value) => value.value as T0;
    public static implicit operator T1(CompositeSyntax<T, T0, T1, T2, T3, T4> value) => value.value as T1;
    public static implicit operator T2(CompositeSyntax<T, T0, T1, T2, T3, T4> value) => value.value as T2;
    public static implicit operator T3(CompositeSyntax<T, T0, T1, T2, T3, T4> value) => value.value as T3;
    public static implicit operator T4(CompositeSyntax<T, T0, T1, T2, T3, T4> value) => value.value as T4;
}

[ExcludeFromCodeCoverage]
internal abstract class CompositeSyntax<T, T0, T1, T2, T3, T4, T5> : CompositeSyntax, Compiler.IParsable<T>
    where T : CompositeSyntax, Compiler.IParsable<T>, new()
    where T0 : Syntax, Compiler.IParsable<T0>
    where T1 : Syntax, Compiler.IParsable<T1>
    where T2 : Syntax, Compiler.IParsable<T2>
    where T3 : Syntax, Compiler.IParsable<T3>
    where T4 : Syntax, Compiler.IParsable<T4>
    where T5 : Syntax, Compiler.IParsable<T5>
{
    public static T Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = T0.Parse(ref parser)
            ?? T1.Parse(ref parser)
            ?? T2.Parse(ref parser)
            ?? T3.Parse(ref parser)
            ?? T4.Parse(ref parser)
            ?? T5.Parse(ref parser) as Syntax;

        if (syntax is null) return null;

        return new T { value = syntax, Source = parser.Commit(ref context) };
    }

    public static implicit operator T0(CompositeSyntax<T, T0, T1, T2, T3, T4, T5> value) => value.value as T0;
    public static implicit operator T1(CompositeSyntax<T, T0, T1, T2, T3, T4, T5> value) => value.value as T1;
    public static implicit operator T2(CompositeSyntax<T, T0, T1, T2, T3, T4, T5> value) => value.value as T2;
    public static implicit operator T3(CompositeSyntax<T, T0, T1, T2, T3, T4, T5> value) => value.value as T3;
    public static implicit operator T4(CompositeSyntax<T, T0, T1, T2, T3, T4, T5> value) => value.value as T4;
    public static implicit operator T5(CompositeSyntax<T, T0, T1, T2, T3, T4, T5> value) => value.value as T5;
}

[ExcludeFromCodeCoverage]
internal abstract class CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6> : CompositeSyntax, Compiler.IParsable<T>
    where T : CompositeSyntax, Compiler.IParsable<T>, new()
    where T0 : Syntax, Compiler.IParsable<T0>
    where T1 : Syntax, Compiler.IParsable<T1>
    where T2 : Syntax, Compiler.IParsable<T2>
    where T3 : Syntax, Compiler.IParsable<T3>
    where T4 : Syntax, Compiler.IParsable<T4>
    where T5 : Syntax, Compiler.IParsable<T5>
    where T6 : Syntax, Compiler.IParsable<T6>
{
    public static T Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = T0.Parse(ref parser)
            ?? T1.Parse(ref parser)
            ?? T2.Parse(ref parser)
            ?? T3.Parse(ref parser)
            ?? T4.Parse(ref parser)
            ?? T5.Parse(ref parser)
            ?? T6.Parse(ref parser) as Syntax;

        if (syntax is null) return null;

        return new T { value = syntax, Source = parser.Commit(ref context) };
    }

    public static implicit operator T0(CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6> value) => value.value as T0;
    public static implicit operator T1(CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6> value) => value.value as T1;
    public static implicit operator T2(CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6> value) => value.value as T2;
    public static implicit operator T3(CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6> value) => value.value as T3;
    public static implicit operator T4(CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6> value) => value.value as T4;
    public static implicit operator T5(CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6> value) => value.value as T5;
    public static implicit operator T6(CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6> value) => value.value as T6;
}

[ExcludeFromCodeCoverage]
internal abstract class CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6, T7> : CompositeSyntax, Compiler.IParsable<T>
    where T : CompositeSyntax, Compiler.IParsable<T>, new()
    where T0 : Syntax, Compiler.IParsable<T0>
    where T1 : Syntax, Compiler.IParsable<T1>
    where T2 : Syntax, Compiler.IParsable<T2>
    where T3 : Syntax, Compiler.IParsable<T3>
    where T4 : Syntax, Compiler.IParsable<T4>
    where T5 : Syntax, Compiler.IParsable<T5>
    where T6 : Syntax, Compiler.IParsable<T6>
    where T7 : Syntax, Compiler.IParsable<T7>
{
    public static T Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = T0.Parse(ref parser)
            ?? T1.Parse(ref parser)
            ?? T2.Parse(ref parser)
            ?? T3.Parse(ref parser)
            ?? T4.Parse(ref parser)
            ?? T5.Parse(ref parser)
            ?? T6.Parse(ref parser)
            ?? T7.Parse(ref parser) as Syntax;

        if (syntax is null) return null;

        return new T { value = syntax, Source = parser.Commit(ref context) };
    }

    public static implicit operator T0(CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6, T7> value) => value.value as T0;
    public static implicit operator T1(CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6, T7> value) => value.value as T1;
    public static implicit operator T2(CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6, T7> value) => value.value as T2;
    public static implicit operator T3(CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6, T7> value) => value.value as T3;
    public static implicit operator T4(CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6, T7> value) => value.value as T4;
    public static implicit operator T5(CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6, T7> value) => value.value as T5;
    public static implicit operator T6(CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6, T7> value) => value.value as T6;
    public static implicit operator T7(CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6, T7> value) => value.value as T7;
}

internal abstract class CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6, T7, T8> : CompositeSyntax, Compiler.IParsable<T>
    where T : CompositeSyntax, Compiler.IParsable<T>, new()
    where T0 : Syntax, Compiler.IParsable<T0>
    where T1 : Syntax, Compiler.IParsable<T1>
    where T2 : Syntax, Compiler.IParsable<T2>
    where T3 : Syntax, Compiler.IParsable<T3>
    where T4 : Syntax, Compiler.IParsable<T4>
    where T5 : Syntax, Compiler.IParsable<T5>
    where T6 : Syntax, Compiler.IParsable<T6>
    where T7 : Syntax, Compiler.IParsable<T7>
    where T8 : Syntax, Compiler.IParsable<T8>
{
    public static T Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = T0.Parse(ref parser)
            ?? T1.Parse(ref parser)
            ?? T2.Parse(ref parser)
            ?? T3.Parse(ref parser)
            ?? T4.Parse(ref parser)
            ?? T5.Parse(ref parser)
            ?? T6.Parse(ref parser)
            ?? T7.Parse(ref parser)
            ?? T8.Parse(ref parser) as Syntax;

        if (syntax is null) return null;

        return new T { value = syntax, Source = parser.Commit(ref context) };
    }

    public static implicit operator T0(CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6, T7, T8> value) => value.value as T0;
    public static implicit operator T1(CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6, T7, T8> value) => value.value as T1;
    public static implicit operator T2(CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6, T7, T8> value) => value.value as T2;
    public static implicit operator T3(CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6, T7, T8> value) => value.value as T3;
    public static implicit operator T4(CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6, T7, T8> value) => value.value as T4;
    public static implicit operator T5(CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6, T7, T8> value) => value.value as T5;
    public static implicit operator T6(CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6, T7, T8> value) => value.value as T6;
    public static implicit operator T7(CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6, T7, T8> value) => value.value as T7;
    public static implicit operator T8(CompositeSyntax<T, T0, T1, T2, T3, T4, T5, T6, T7, T8> value) => value.value as T8;
}

/*

[+ means and/or]

x declare function - modifiers 'function' identifier scope
x declare datatype - modifiers 'datatype' identifier { '=' reference } (optional) [algebra] scope
x declare datum - declarator parameter
x identifier - name + parameters ...
x reference - name + arguments ...
x import - 'import' name
x partof - 'part of' name

x declarator - 'var' or 'constant' or 'let'
x modifiers - 'optional' or 'compiled' or 'persistent' or 'shared'
x name - word + wordable symbol ... [symbols don't need to be separated]
x assignment - name '=' value
x parameter - explicit - name => modifiers reference [datatype] { '=' value } (optionally) [initializer]
            - implicit - assignment
x scalar - literal ...
x value - scalar or reference or scope

x error - all until ';'

aggregates
x arguments - '(' value, ... ')'
x index - '[' value,... ']'
x scope - '{' value; ... '}'
x parameters - '(' parameter + assignment, ... ')'

*/