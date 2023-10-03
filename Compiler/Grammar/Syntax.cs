// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System;

namespace Ronin.Grammar;

internal interface IParsableSyntax<T> where T : IParsableSyntax<T>
{
    static abstract T Parse(ref Parser current);
}

internal abstract class Syntax
{
    protected internal ReadOnlyMemory<Token> Source { get; init; }

    public override bool Equals(object obj) => (obj as Syntax)?.Source.Span.SequenceEqual(Source.Span) ?? false;

    public override int GetHashCode() => Source.Span.ToHashCode();
}

internal abstract class UnionSyntax<T, T0, T1> : Syntax, IParsableSyntax<T>
    where T : UnionSyntax<T, T0, T1>, IParsableSyntax<T>, new()
    where T0 : Syntax, IParsableSyntax<T0>
    where T1 : Syntax, IParsableSyntax<T1>
{
    public static T Parse(ref Parser current)
    {
        Parser parser = current;

        var syntax = T0.Parse(ref parser) ?? T1.Parse(ref parser) as Syntax;

        if (syntax is null) return null;

        return new T 
        { 
            value = syntax, 
            Source = parser.Commit(ref current) 
        };
    }

    public override bool Equals(object obj) => value?.Equals(obj) ?? false;

    public override int GetHashCode() => value.GetHashCode();

    public static implicit operator T0(UnionSyntax<T, T0, T1> value) => value.value as T0;
    public static implicit operator T1(UnionSyntax<T, T0, T1> value) => value.value as T1;

    protected internal Syntax value;
}

internal abstract class UnionSyntax<T, T0, T1, T2> : UnionSyntax<T, T0, T1>
    where T : UnionSyntax<T, T0, T1, T2>, new()
    where T0 : Syntax, IParsableSyntax<T0>
    where T1 : Syntax, IParsableSyntax<T1>
    where T2 : Syntax, IParsableSyntax<T2>
{
    public static new T Parse(ref Parser current) => UnionSyntax<T, T0, T1>.Parse(ref current) ?? T2.Parse(ref current) as T;

    public static implicit operator T2(UnionSyntax<T, T0, T1, T2> value) => value.value as T2;
}