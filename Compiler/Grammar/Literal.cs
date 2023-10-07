// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System;

namespace Ronin.Grammar;

/// <summary>
///     A <see cref="Constant"/>, <see cref="Compiled"/> value written directly in code
/// </summary>
/// 
/// <example>
///     var when = 12:33p;
///                ↑↑↑↑↑↑
///     constant cash = $75;
///                     ↑↑↑
///     let x = 7,000,876 + cash amount;
///             ↑↑↑↑↑↑↑↑↑
/// </example>
internal class Literal : Value.Temporary
{
    public ReadOnlyMemory<Token> Tokens { get; init; }

    public new static Literal Parse(ref Parser current)
    {
        Parser parser = current;

        if (parser.TryAdvanceMany<Lexicon.Literal>() is false) return null;

        return new Literal { Tokens = current.AdvanceTo(parser) };
    }
}