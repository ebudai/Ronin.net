// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal abstract class Syntax
{
    protected internal ReadOnlyMemory<Token> Source { get; init; }
}

internal abstract class CompositeSyntax<T, T0, T1> : Syntax, IParsableSyntax<T>
    where T : CompositeSyntax<T, T0, T1>, IParsableSyntax<T>, new()
    where T0 : Syntax, IParsableSyntax<T0>
    where T1 : Syntax, IParsableSyntax<T1>
{
    public static T Parse(scoped ref Parser current)
    {
        Parser parser = current;

        var syntax = T0.Parse(ref parser) ?? T1.Parse(ref parser) as Syntax;

        if (syntax is null) return null;

        return new T { value = syntax, Source = parser.Commit(ref current) };
    }

    public static implicit operator T0(CompositeSyntax<T, T0, T1> value) => value.value as T0;
    public static implicit operator T1(CompositeSyntax<T, T0, T1> value) => value.value as T1;

    protected internal Syntax value;
}

internal abstract class CompositeSyntax<T, T0, T1, T2> : CompositeSyntax<T, T0, T1>, IParsableSyntax<T>
    where T : CompositeSyntax<T, T0, T1, T2>, IParsableSyntax<T>, new()
    where T0 : Syntax, IParsableSyntax<T0>
    where T1 : Syntax, IParsableSyntax<T1>
    where T2 : Syntax, IParsableSyntax<T2>
{
    public new static T Parse(scoped ref Parser current)
    {
        var composite = CompositeSyntax<T, T0, T1>.Parse(ref current);
        if (composite is not null) return composite;

        Parser parser = current;

        var syntax = T2.Parse(ref parser);
        if (syntax is null) return null;

        return new T { value = syntax, Source = parser.Commit(ref current) };
    }

    public static implicit operator T2(CompositeSyntax<T, T0, T1, T2> value) => value.value as T2;
}