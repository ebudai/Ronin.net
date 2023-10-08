// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System;

namespace Ronin.Grammar;

/// <summary>
///     Modifies a <see cref="Type"/> or used to restrict a <see cref="Datum"/> or a <see cref="Function"/>
/// </summary>
/// 
/// <remarks>Currently limited to <see cref="Compiled"/>, <see cref="Persistent"/>, <see cref="Global"/>, and <see cref="Optional"/></remarks>
internal class Modifiers
{
    public ReadOnlyMemory<Token> Tokens { get; init; }

    public bool Is<T>() where T : Modifier
    {
        foreach (var token in Tokens.Span)
        {
            if (token is T) return true;
        }
        return false;
    }

    public static Modifiers Parse(ref Parser current)
    {
        Parser parser = current;

        if (parser.TryAdvanceMany<Modifier>() is false) return null;

        return new Modifiers { Tokens = current.AdvanceTo(parser) };
    }
}
