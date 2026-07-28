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
internal class Literal : Temporary
{
    public ReadOnlyMemory<Token> Tokens { get; init; }

    public new static Literal Parse(ref Parser current)
    {
        Parser parser = current;

        // Exactly one. Juxtaposition can never be meaningful between two
        // literals — a pattern must begin with a word, so none can ever match
        // «1 2» — and the resolver agrees: two atoms with no operator between
        // them does not parse. A multi-token literal like a date is the lexer's
        // business and arrives as one token already.
        if (parser.TryAdvance<Lexicon.Literal>() is false) return null;

        return new Literal { Tokens = current.AdvanceTo(parser) };
    }
}
